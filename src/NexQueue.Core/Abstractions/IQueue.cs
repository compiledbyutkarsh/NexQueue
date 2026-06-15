using System.Threading;
using System.Threading.Tasks;
using NexQueue.Core.Models;

namespace NexQueue.Core.Abstractions;

public interface IQueue
{
    string Name     { get; }
    long   Count    { get; }

    ValueTask EnqueueAsync(TaskMessage message, CancellationToken ct = default);
    ValueTask<TaskMessage?> DequeueAsync(CancellationToken ct = default);
    ValueTask<TaskMessage?> PeekAsync(CancellationToken ct = default);
    ValueTask AckAsync(string taskId, CancellationToken ct = default);
    ValueTask NackAsync(string taskId, string error, CancellationToken ct = default);
    QueueStats GetStats();
}

public interface IBroker
{
    ValueTask EnqueueAsync(TaskMessage message, CancellationToken ct = default);
    ValueTask<TaskMessage?> DequeueAsync(string queue, CancellationToken ct = default);
    ValueTask AckAsync(string queue, string taskId, CancellationToken ct = default);
    ValueTask NackAsync(string queue, string taskId, string error, CancellationToken ct = default);
    ValueTask<TaskMessage?> GetTaskAsync(string taskId, CancellationToken ct = default);
    BrokerStats GetStats();
    System.Collections.Generic.IReadOnlyList<string> GetQueueNames();
}

public interface ITaskHandler
{
    string TaskType { get; }
    Task<TaskResult> HandleAsync(TaskMessage message, CancellationToken ct = default);
}

public interface IWorker
{
    string   WorkerId  { get; }
    bool     IsRunning { get; }
    int      Processed { get; }

    Task StartAsync(CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);
}
