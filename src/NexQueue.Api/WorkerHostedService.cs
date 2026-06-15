using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexQueue.Core.Abstractions;
using NexQueue.Worker.Core;

namespace NexQueue.Api;

public sealed class WorkerHostedService : IHostedService
{
    private readonly IBroker              _broker;
    private readonly IEnumerable<ITaskHandler> _handlers;
    private readonly ILogger<WorkerHostedService> _logger;
    private readonly ILoggerFactory       _loggerFactory;
    private readonly List<NexWorker>      _workers = new();
    private CancellationTokenSource?      _cts;

    public WorkerHostedService(
        IBroker broker,
        IEnumerable<ITaskHandler> handlers,
        ILogger<WorkerHostedService> logger,
        ILoggerFactory loggerFactory)
    {
        _broker        = broker;
        _handlers      = handlers;
        _logger        = logger;
        _loggerFactory = loggerFactory;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        for (int i = 0; i < 3; i++)
        {
            var worker = new NexWorker(
                _broker,
                _handlers,
                _loggerFactory.CreateLogger<NexWorker>(),
                queue: "default",
                concurrency: 4
            );
            _workers.Add(worker);
            await worker.StartAsync(_cts.Token);
        }

        _logger.LogInformation("NexQueue: {Count} workers started", _workers.Count);
    }

    public async Task StopAsync(CancellationToken ct)
    {
        _cts?.Cancel();
        foreach (var worker in _workers)
        {
            await worker.StopAsync(ct);
        }
        _logger.LogInformation("NexQueue: all workers stopped");
    }
}
