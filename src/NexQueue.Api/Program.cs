using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexQueue.Broker.Core;
using NexQueue.Core.Abstractions;
using NexQueue.Worker.Core;
using NexQueue.Worker.Handlers;
using NexQueue.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "NexQueue API", Version = "v1" });
});

builder.Services.AddSingleton<NexBroker>();
builder.Services.AddSingleton<IBroker>(sp => sp.GetRequiredService<NexBroker>());

builder.Services.AddSingleton<ITaskHandler, EmailHandler>();
builder.Services.AddSingleton<ITaskHandler, ImageProcessHandler>();
builder.Services.AddSingleton<ITaskHandler, DataExportHandler>();
builder.Services.AddSingleton<ITaskHandler, FailingHandler>();

builder.Services.AddHostedService<WorkerHostedService>();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "NexQueue v1"));

app.MapControllers();

app.MapGet("/health", () => new
{
    status    = "healthy",
    service   = "NexQueue",
    timestamp = DateTimeOffset.UtcNow
});

app.Run();
