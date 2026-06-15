using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NexQueue.Core.Abstractions;
using NexQueue.Core.Models;

namespace NexQueue.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class TasksController : ControllerBase
{
    private readonly IBroker _broker;

    public TasksController(IBroker broker) => _broker = broker;

    [HttpPost]
    public async Task<IActionResult> Enqueue([FromBody] EnqueueRequest req, CancellationToken ct)
    {
        var message = new TaskMessage
        {
            Type       = req.Type,
            Queue      = req.Queue ?? "default",
            Payload    = req.Payload ?? "{}",
            Priority   = req.Priority,
            MaxRetries = req.MaxRetries,
            Timeout    = TimeSpan.FromSeconds(req.TimeoutSeconds)
        };

        await _broker.EnqueueAsync(message, ct);
        return Accepted(new { message.Id, message.Queue, message.Type, message.Priority });
    }

    [HttpGet("{taskId}")]
    public async Task<IActionResult> GetTask(string taskId, CancellationToken ct)
    {
        var task = await _broker.GetTaskAsync(taskId, ct);
        if (task is null) return NotFound(new { taskId, error = "Task not found" });
        return Ok(task);
    }

    [HttpGet("stats")]
    public IActionResult GetStats() => Ok(_broker.GetStats());

    [HttpGet("queues")]
    public IActionResult GetQueues() => Ok(_broker.GetQueueNames());
}

public sealed record EnqueueRequest(
    string       Type,
    string?      Queue          = "default",
    string?      Payload        = "{}",
    TaskPriority Priority       = TaskPriority.Normal,
    int          MaxRetries     = 3,
    int          TimeoutSeconds = 300
);
