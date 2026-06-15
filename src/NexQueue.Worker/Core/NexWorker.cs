using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NexQueue.Core.Abstractions;
using NexQueue.Core.Models;

namespace NexQueue.Worker.Core;

public sealed class NexWorker : IWorker
{
    private readonly IBroker                                     _broker;
    private readonly ConcurrentDictionary<string, ITaskHandler> _handlers;
    private readonly ILogger<NexWorker>                          _logger;
    private readonly string                                      _queue;
    private readonly SemaphoreSlim                               _concurrency;
    private CancellationTokenSource?                             _cts;
    private Task?                                                _loop;
    private int _processed;
    private int _failed;

    public string WorkerId  { get; }
    public bool   IsRunning { get; private set; }
    public int    Processed => _processed;
    public int    Failed    => _failed;

    public NexWorker(
        IBroker broker,
        IEnumerable<ITaskHandler> handlers,
        ILogger<NexWorker> logger,
        string queue       = "default",
        int    concurrency = 4)
    {
        _broker      = broker;
        _logger      = logger;
        _queue       = queue;
        _concurrency = new SemaphoreSlim(concurrency, concurrency);
        WorkerId     = $"worker-{Guid.NewGuid():N}"[..15];

        _handlers = new ConcurrentDictionary<string, ITaskHandler>(StringComparer.OrdinalIgnoreCase);
        foreach (var h in handlers)
        {
            _handlers[h.TaskType] = h;
            _logger.LogInformation("[{Worker}] Registered handler: {Type}", WorkerId, h.TaskType);
        }
    }

    public Task StartAsync(CancellationToken ct = default)
    {
        _cts     = CancellationTokenSource.CreateLinkedTokenSource(ct);
        IsRunning = true;
        _loop    = RunLoopAsync(_cts.Token);
        _logger.LogInformation("[{Worker}] Started on queue '{Queue}'", WorkerId, _queue);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        IsRunning = false;
        _cts?.Cancel();
        if (_loop is not null)
        {
            try { await _loop.WaitAsync(ct); }
            catch (OperationCanceledException) { }
        }
        _logger.LogInformation("[{Worker}] Stopped. Processed: {Count}", WorkerId, _processed);
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await _concurrency.WaitAsync(ct).ConfigureAwait(false);

                var message = await _broker.DequeueAsync(_queue, ct).ConfigureAwait(false);
                if (message is null)
                {
                    _concurrency.Release();
                    continue;
                }

                _ = ProcessAsync(message, ct).ContinueWith(_ => _concurrency.Release(), ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Worker}] Loop error", WorkerId);
                await Task.Delay(1000, ct).ConfigureAwait(false);
            }
        }
    }

    private async Task ProcessAsync(TaskMessage message, CancellationToken ct)
    {
        message.WorkerId = WorkerId;
        var sw = Stopwatch.StartNew();

        _logger.LogInformation("[{Worker}] Processing {Id} ({Type})", WorkerId, message.Id, message.Type);

        if (!_handlers.TryGetValue(message.Type, out var handler))
        {
            var err = $"No handler registered for task type '{message.Type}'";
            _logger.LogWarning("[{Worker}] {Error}", WorkerId, err);
            await _broker.NackAsync(_queue, message.Id, err, ct).ConfigureAwait(false);
            Interlocked.Increment(ref _failed);
            return;
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(message.Timeout);

        try
        {
            var result = await handler.HandleAsync(message, timeout.Token).ConfigureAwait(false);
            sw.Stop();

            if (result.Success)
            {
                await _broker.AckAsync(_queue, message.Id, ct).ConfigureAwait(false);
                Interlocked.Increment(ref _processed);
                _logger.LogInformation("[{Worker}] Completed {Id} in {Ms}ms", WorkerId, message.Id, sw.ElapsedMilliseconds);
            }
            else
            {
                await _broker.NackAsync(_queue, message.Id, result.Error ?? "Handler returned failure", ct).ConfigureAwait(false);
                Interlocked.Increment(ref _failed);
                _logger.LogWarning("[{Worker}] Failed {Id}: {Error}", WorkerId, message.Id, result.Error);
            }
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            var err = $"Task timed out after {message.Timeout.TotalSeconds}s";
            await _broker.NackAsync(_queue, message.Id, err, ct).ConfigureAwait(false);
            Interlocked.Increment(ref _failed);
            _logger.LogWarning("[{Worker}] Timeout {Id}", WorkerId, message.Id);
        }
        catch (Exception ex)
        {
            sw.Stop();
            await _broker.NackAsync(_queue, message.Id, ex.Message, ct).ConfigureAwait(false);
            Interlocked.Increment(ref _failed);
            _logger.LogError(ex, "[{Worker}] Exception on {Id}", WorkerId, message.Id);
        }
    }
}
