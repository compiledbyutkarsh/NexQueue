using System;
using System.Collections.Generic;

namespace NexQueue.Core.Models;

public enum TaskPriority
{
    Low      = 0,
    Normal   = 1,
    High     = 2,
    Critical = 3
}

public enum TaskStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Retrying,
    DeadLettered
}

public sealed class TaskMessage
{
    public string              Id          { get; init; } = Guid.NewGuid().ToString("N");
    public string              Type        { get; init; } = string.Empty;
    public string              Queue       { get; init; } = "default";
    public string              Payload     { get; init; } = string.Empty;
    public TaskPriority        Priority    { get; init; } = TaskPriority.Normal;
    public TaskStatus          Status      { get; set;  } = TaskStatus.Pending;
    public int                 MaxRetries  { get; init; } = 3;
    public int                 RetryCount  { get; set;  } = 0;
    public TimeSpan            Timeout     { get; init; } = TimeSpan.FromMinutes(5);
    public DateTimeOffset      CreatedAt   { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset?     ScheduledAt { get; init; }
    public DateTimeOffset?     StartedAt   { get; set;  }
    public DateTimeOffset?     CompletedAt { get; set;  }
    public string?             Error       { get; set;  }
    public string?             WorkerId    { get; set;  }
    public Dictionary<string, string> Headers { get; init; } = new();

    public bool IsScheduled  => ScheduledAt.HasValue && ScheduledAt > DateTimeOffset.UtcNow;
    public bool CanRetry     => RetryCount < MaxRetries;
    public bool IsTerminal   => Status is TaskStatus.Completed or TaskStatus.DeadLettered;

    public TimeSpan? ProcessingTime => CompletedAt.HasValue && StartedAt.HasValue
        ? CompletedAt.Value - StartedAt.Value
        : null;
}
