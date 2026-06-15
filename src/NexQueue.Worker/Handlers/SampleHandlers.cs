using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NexQueue.Core.Abstractions;
using NexQueue.Core.Models;

namespace NexQueue.Worker.Handlers;

public sealed class EmailHandler : ITaskHandler
{
    private readonly ILogger<EmailHandler> _logger;
    public string TaskType => "email.send";

    public EmailHandler(ILogger<EmailHandler> logger) => _logger = logger;

    public async Task<TaskResult> HandleAsync(TaskMessage message, CancellationToken ct = default)
    {
        var start = DateTimeOffset.UtcNow;
        await Task.Delay(Random.Shared.Next(50, 200), ct);

        var payload = JsonSerializer.Deserialize<EmailPayload>(message.Payload);
        if (payload is null)
            return TaskResult.Fail(message.Id, DateTimeOffset.UtcNow - start, "Invalid payload");

        _logger.LogInformation("Email sent to {To}: {Subject}", payload.To, payload.Subject);
        return TaskResult.Ok(message.Id, DateTimeOffset.UtcNow - start, $"Email delivered to {payload.To}");
    }

    private record EmailPayload(string To, string Subject, string Body);
}

public sealed class ImageProcessHandler : ITaskHandler
{
    private readonly ILogger<ImageProcessHandler> _logger;
    public string TaskType => "image.process";

    public ImageProcessHandler(ILogger<ImageProcessHandler> logger) => _logger = logger;

    public async Task<TaskResult> HandleAsync(TaskMessage message, CancellationToken ct = default)
    {
        var start = DateTimeOffset.UtcNow;
        await Task.Delay(Random.Shared.Next(100, 500), ct);

        _logger.LogInformation("Image processed: {Payload}", message.Payload);
        return TaskResult.Ok(message.Id, DateTimeOffset.UtcNow - start, "Image resized and compressed");
    }
}

public sealed class DataExportHandler : ITaskHandler
{
    private readonly ILogger<DataExportHandler> _logger;
    public string TaskType => "data.export";

    public DataExportHandler(ILogger<DataExportHandler> logger) => _logger = logger;

    public async Task<TaskResult> HandleAsync(TaskMessage message, CancellationToken ct = default)
    {
        var start = DateTimeOffset.UtcNow;

        for (int i = 0; i < 5; i++)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(100, ct);
            _logger.LogDebug("Export progress: {Pct}%", (i + 1) * 20);
        }

        return TaskResult.Ok(message.Id, DateTimeOffset.UtcNow - start, "Export complete: 1000 records");
    }
}

public sealed class FailingHandler : ITaskHandler
{
    public string TaskType => "task.fail";

    public Task<TaskResult> HandleAsync(TaskMessage message, CancellationToken ct = default)
        => Task.FromResult(TaskResult.Fail(message.Id, TimeSpan.Zero, "Simulated failure for retry testing"));
}
