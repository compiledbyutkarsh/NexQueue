using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NexQueue.Core.Abstractions;
using NexQueue.Core.Models;
using NexQueue.Broker.Queues;

namespace NexQueue.Broker.Core;

public sealed class NexBroker : IBroker
{
    private readonly ConcurrentDictionary<string, InMemoryQueue> _queues = new();
    private readonly ILogger<NexBroker>                          _logger;
    private int _activeWorkers;

    public NexBroker(ILogger<NexBroker> logger)
    {
        _logger = logger;
        EnsureQueue("default");
        EnsureQueue("dead-letter");
    }

    private InMemoryQueue EnsureQueue(string name)
        => _queues.GetOrAdd(name, n =>
        {
            _logger.LogInformation("Queue created: {Queue}", n);
            return new InMemoryQueue(n);
        });

    public ValueTask EnqueueAsync(TaskMessage message, CancellationToken ct = default)
    {
        var queue = EnsureQueue(message.Queue);
        _logger.LogDebug("Enqueued task {Id} ({Type}) -> {Queue} [{Priority}]",
            message.Id, message.Type, message.Queue, message.Priority);
        return queue.EnqueueAsync(message, ct);
    }

    public ValueTask<TaskMessage?> DequeueAsync(string queue, CancellationToken ct = default)
        => EnsureQueue(queue).DequeueAsync(ct);

    public ValueTask AckAsync(string queue, string taskId, CancellationToken ct = default)
    {
        _logger.LogDebug("ACK task {Id} on {Queue}", taskId, queue);
        return EnsureQueue(queue).AckAsync(taskId, ct);
    }

    public ValueTask NackAsync(string queue, string taskId, string error, CancellationToken ct = default)
    {
        _logger.LogWarning("NACK task {Id} on {Queue}: {Error}", taskId, queue, error);
        return EnsureQueue(queue).NackAsync(taskId, error, ct);
    }

    public ValueTask<TaskMessage?> GetTaskAsync(string taskId, CancellationToken ct = default)
    {
        foreach (var queue in _queues.Values)
        {
            var msg = queue.GetTask(taskId);
            if (msg is not null) return ValueTask.FromResult<TaskMessage?>(msg);
        }
        return ValueTask.FromResult<TaskMessage?>(null);
    }

    public void RegisterWorker()   => Interlocked.Increment(ref _activeWorkers);
    public void UnregisterWorker() => Interlocked.Decrement(ref _activeWorkers);

    public BrokerStats GetStats()
    {
        var queueStats = _queues.ToDictionary(k => k.Key, v => v.Value.GetStats());
        return new BrokerStats
        {
            TotalQueues       = _queues.Count,
            TotalPending      = queueStats.Values.Sum(s => s.Pending),
            TotalRunning      = queueStats.Values.Sum(s => s.Running),
            TotalCompleted    = queueStats.Values.Sum(s => s.Completed),
            TotalFailed       = queueStats.Values.Sum(s => s.Failed),
            TotalDeadLettered = queueStats.Values.Sum(s => s.DeadLettered),
            ActiveWorkers     = _activeWorkers,
            Queues            = queueStats
        };
    }

    public IReadOnlyList<string> GetQueueNames()
        => _queues.Keys.ToList();
}
