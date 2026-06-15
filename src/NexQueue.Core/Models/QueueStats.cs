using System;
using System.Collections.Generic;

namespace NexQueue.Core.Models;

public sealed class QueueStats
{
    public string                        QueueName      { get; init; } = string.Empty;
    public long                          Pending        { get; init; }
    public long                          Running        { get; init; }
    public long                          Completed      { get; init; }
    public long                          Failed         { get; init; }
    public long                          DeadLettered   { get; init; }
    public long                          TotalProcessed { get; init; }
    public double                        AvgProcessingMs { get; init; }
    public double                        Throughput     { get; init; }
    public DateTimeOffset                Timestamp      { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class BrokerStats
{
    public int                           TotalQueues    { get; init; }
    public long                          TotalPending   { get; init; }
    public long                          TotalRunning   { get; init; }
    public long                          TotalCompleted { get; init; }
    public long                          TotalFailed    { get; init; }
    public long                          TotalDeadLettered { get; init; }
    public int                           ActiveWorkers  { get; init; }
    public Dictionary<string, QueueStats> Queues        { get; init; } = new();
    public DateTimeOffset                Timestamp      { get; init; } = DateTimeOffset.UtcNow;
}
