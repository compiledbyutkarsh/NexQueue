using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NexQueue.Core.Abstractions;
using NexQueue.Core.Models;
using TaskStatus = NexQueue.Core.Models.TaskStatus;

namespace NexQueue.Broker.Queues;

internal sealed class InMemoryQueue : IQueue
{
    private readonly PriorityQueue<TaskMessage, int>    _queue     = new();
    private readonly ConcurrentDictionary<string, TaskMessage> _inflight = new();
    private readonly ConcurrentDictionary<string, TaskMessage> _all      = new();
    private readonly SemaphoreSlim                      _signal    = new(0);
    private readonly Lock                               _lock      = new();

    private long _completed;
    private long _failed;
    private long _deadLettered;
    private long _totalProcessingMs;
    private long _totalProcessed;

    public string Name  { get; }
    public long   Count { get { lock (_lock) { return _queue.Count; } } }

    public InMemoryQueue(string name) => Name = name;

    public ValueTask EnqueueAsync(TaskMessage message, CancellationToken ct = default)
    {
        lock (_lock)
        {
            int priority = -(int)message.Priority;
            _queue.Enqueue(message, priority);
            _all[message.Id] = message;
        }
        _signal.Release();
        return ValueTask.CompletedTask;
    }

    public async ValueTask<TaskMessage?> DequeueAsync(CancellationToken ct = default)
    {
        await _signal.WaitAsync(ct).ConfigureAwait(false);

        lock (_lock)
        {
            if (!_queue.TryDequeue(out var message, out _))
                return null;

            if (message.IsScheduled)
            {
                _queue.Enqueue(message, -(int)message.Priority);
                _signal.Release();
                return null;
            }

            message.Status    = TaskStatus.Running;
            message.StartedAt = DateTimeOffset.UtcNow;
            _inflight[message.Id] = message;
            _all[message.Id]      = message;
            return message;
        }
    }

    public ValueTask<TaskMessage?> PeekAsync(CancellationToken ct = default)
    {
        lock (_lock)
        {
            _queue.TryPeek(out var message, out _);
            return ValueTask.FromResult(message);
        }
    }

    public ValueTask AckAsync(string taskId, CancellationToken ct = default)
    {
        if (_inflight.TryRemove(taskId, out var message))
        {
            message.Status      = TaskStatus.Completed;
            message.CompletedAt = DateTimeOffset.UtcNow;
            _all[taskId]        = message;

            if (message.StartedAt.HasValue)
            {
                var ms = (long)(message.CompletedAt.Value - message.StartedAt.Value).TotalMilliseconds;
                Interlocked.Add(ref _totalProcessingMs, ms);
            }

            Interlocked.Increment(ref _completed);
            Interlocked.Increment(ref _totalProcessed);
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask NackAsync(string taskId, string error, CancellationToken ct = default)
    {
        if (_inflight.TryRemove(taskId, out var message))
        {
            message.Error      = error;
            message.RetryCount++;

            if (message.CanRetry)
            {
                message.Status = TaskStatus.Retrying;
                lock (_lock)
                {
                    _queue.Enqueue(message, -(int)message.Priority);
                }
                _signal.Release();
            }
            else
            {
                message.Status      = TaskStatus.DeadLettered;
                message.CompletedAt = DateTimeOffset.UtcNow;
                Interlocked.Increment(ref _deadLettered);
                Interlocked.Increment(ref _totalProcessed);
            }

            _all[taskId] = message;
            Interlocked.Increment(ref _failed);
        }
        return ValueTask.CompletedTask;
    }

    public TaskMessage? GetTask(string taskId)
        => _all.TryGetValue(taskId, out var msg) ? msg : null;

    public QueueStats GetStats()
    {
        long pending, running;
        lock (_lock) { pending = _queue.Count; }
        running = _inflight.Count;

        long completed    = Interlocked.Read(ref _completed);
        long failed       = Interlocked.Read(ref _failed);
        long deadLettered = Interlocked.Read(ref _deadLettered);
        long processed    = Interlocked.Read(ref _totalProcessed);
        long totalMs      = Interlocked.Read(ref _totalProcessingMs);

        return new QueueStats
        {
            QueueName       = Name,
            Pending         = pending,
            Running         = running,
            Completed       = completed,
            Failed          = failed,
            DeadLettered    = deadLettered,
            TotalProcessed  = processed,
            AvgProcessingMs = processed > 0 ? (double)totalMs / processed : 0
        };
    }
}
