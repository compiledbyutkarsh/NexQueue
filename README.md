# ⚡ NexQueue

> A distributed task queue system built from scratch in C# .NET 9. Priority queues, concurrent workers, retry logic, dead-letter queue, and a REST API — all in-process, zero external dependencies.

![Language](https://img.shields.io/badge/Language-C%23%20.NET%209-purple?style=flat-square)
![Architecture](https://img.shields.io/badge/Architecture-Distributed-blue?style=flat-square)
![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)
![Status](https://img.shields.io/badge/Status-Active-brightgreen?style=flat-square)

---

## 🧠 What is NexQueue?

NexQueue is a production-grade distributed task queue built entirely in C# without any external message broker (no RabbitMQ, no Redis, no Kafka). It implements the core concepts behind systems like Celery and BullMQ — priority scheduling, concurrent worker pools, automatic retries, dead-letter queues, and real-time stats — using only .NET 9 primitives.

---

## 🚀 Features

### 📬 Priority Queue Broker
- Four priority levels: Low, Normal, High, Critical
- In-memory priority queue with O(log n) enqueue/dequeue
- Multiple named queues with isolated stats
- Scheduled task support (delay execution to future time)

### ⚡ Concurrent Worker Pool
- Multiple worker instances running in parallel
- Per-worker configurable concurrency (semaphore-based)
- Handler registry — workers auto-route by task type
- Per-task timeout enforcement via CancellationToken

### 🔄 Retry and Dead-Letter
- Configurable max retries per task
- Exponential backoff on failure
- Dead-letter queue for exhausted tasks
- Full error tracking with stack traces

### 📊 REST API
- POST /api/tasks — enqueue a task
- GET /api/tasks/{id} — get task status
- GET /api/tasks/stats — broker-wide statistics
- GET /api/tasks/queues — list all queues
- GET /health — health check endpoint
- Swagger UI included

---

## 🏗️ Architecture

```
HTTP Client
     |
     v
NexQueue.Api  (ASP.NET Core REST API)
     |
     v
NexQueue.Broker  (Priority Queue + Dead-Letter)
     |
     v
NexQueue.Worker  (Concurrent Worker Pool)
     |
     v
ITaskHandler  (email.send | image.process | data.export)
```

---

## 📁 Project Structure

```
NexQueue/
├── src/
│   ├── NexQueue.Core/
│   │   ├── Models/          # TaskMessage, TaskResult, QueueStats
│   │   └── Abstractions/    # IBroker, IQueue, IWorker, ITaskHandler
│   ├── NexQueue.Broker/
│   │   ├── Queues/          # InMemoryQueue (priority + inflight tracking)
│   │   └── Core/            # NexBroker (multi-queue manager)
│   ├── NexQueue.Worker/
│   │   ├── Core/            # NexWorker (concurrent processing loop)
│   │   └── Handlers/        # EmailHandler, ImageProcessHandler, DataExportHandler
│   ├── NexQueue.Api/
│   │   ├── Controllers/     # TasksController (REST endpoints)
│   │   ├── Program.cs       # DI setup, middleware
│   │   └── WorkerHostedService.cs
│   └── NexQueue.CLI/        # CLI tool
└── NexQueue.sln
```

---

## 🛠️ Build Requirements

| Tool | Version |
|------|---------|
| .NET SDK | 9.0+ |
| macOS / Linux / Windows | any |

No external dependencies — no Redis, no RabbitMQ, no Docker required.

---

## 🔨 Building and Running

```bash
git clone https://github.com/compiledbyutkarsh/NexQueue
cd NexQueue
dotnet build
dotnet run --project src/NexQueue.Api
```

API starts at http://localhost:5033
Swagger UI at http://localhost:5033/swagger

---

## 💻 Usage

### Enqueue a task

```bash
curl -X POST http://localhost:5033/api/tasks \
  -H "Content-Type: application/json" \
  -d '{"type":"email.send","queue":"default","payload":"{\"to\":\"user@example.com\",\"subject\":\"Hello\",\"body\":\"World\"}","priority":2,"maxRetries":3}'
```

### Check task status

```bash
curl http://localhost:5033/api/tasks/{taskId}
```

### Get broker stats

```bash
curl http://localhost:5033/api/tasks/stats
```

### Built-in task types

| Type | Description |
|------|-------------|
| email.send | Simulates email delivery |
| image.process | Simulates image resizing |
| data.export | Simulates paginated data export |
| task.fail | Always fails (for retry testing) |

---

## 📌 Roadmap

- [ ] Persistent storage backend (SQLite/PostgreSQL)
- [ ] WebSocket real-time dashboard
- [ ] Cron/scheduled task support
- [ ] Rate limiting per queue
- [ ] gRPC API alongside REST
- [ ] Distributed multi-node support

---

## 📜 License

MIT License - free to use, study, and build upon.

---

<p align="center">Made with ⚡ by <a href="https://github.com/compiledbyutkarsh">compiled by utkarsh</a></p>
