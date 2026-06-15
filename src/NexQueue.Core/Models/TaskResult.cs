using System;

namespace NexQueue.Core.Models;

public sealed class TaskResult
{
    public string          TaskId      { get; init; } = string.Empty;
    public bool            Success     { get; init; }
    public string?         Output      { get; init; }
    public string?         Error       { get; init; }
    public DateTimeOffset  CompletedAt { get; init; } = DateTimeOffset.UtcNow;
    public TimeSpan        Duration    { get; init; }

    public static TaskResult Ok(string taskId, TimeSpan duration, string? output = null) => new()
    {
        TaskId      = taskId,
        Success     = true,
        Output      = output,
        Duration    = duration,
        CompletedAt = DateTimeOffset.UtcNow
    };

    public static TaskResult Fail(string taskId, TimeSpan duration, string error) => new()
    {
        TaskId      = taskId,
        Success     = false,
        Error       = error,
        Duration    = duration,
        CompletedAt = DateTimeOffset.UtcNow
    };
}
