# FinFlow — Real-Time Payment Event Pipeline

> A production-pattern .NET microservices system for financial transaction processing, featuring real-time analytics, event-driven architecture, and transactional consistency guarantees.

![.NET](https://img.shields.io/badge/.NET-6.0-512BD4?style=flat&logo=dotnet)
![Kafka](https://img.shields.io/badge/Kafka-Event_Streaming-231F20?style=flat&logo=apachekafka)
![React](https://img.shields.io/badge/React-18-61DAFB?style=flat&logo=react)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?style=flat&logo=docker)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15-4169E1?style=flat&logo=postgresql)

---

## Overview

FinFlow demonstrates how modern financial platforms handle high-throughput payment processing with **guaranteed delivery**, **eventual consistency**, and **real-time visibility** — all common requirements in production fintech systems.

It is built around three independently deployable .NET microservices communicating asynchronously via **Apache Kafka**, with a React + SignalR dashboard showing live transaction streams.

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                         CLIENT LAYER                                │
│                                                                     │
│   React Dashboard  ←──── SignalR (WebSocket) ────→  .NET API        │
└────────────────────────────────┬────────────────────────────────────┘
                                 │ REST / HTTP
┌────────────────────────────────▼────────────────────────────────────┐
│                      FinFlow.API  (Port 5000)                       │
│                                                                     │
│   • POST /api/transactions    — submit payment                      │
│   • GET  /api/transactions    — query history                       │
│   • SignalR /hubs/dashboard   — push live events to frontend        │
│                                                                     │
│   ┌─────────────────────────────────────────┐                       │
│   │         OUTBOX PATTERN                  │                       │
│   │  Save to DB + OutboxMessage atomically  │                       │
│   │  Background job polls & publishes       │                       │
│   └──────────────┬──────────────────────────┘                       │
└──────────────────┼──────────────────────────────────────────────────┘
                   │ Kafka (transaction.created topic)
    ┌──────────────▼──────────────┐    ┌──────────────────────────────┐
    │   FinFlow.PaymentService    │    │  FinFlow.NotificationService │
    │       (Port 5001)           │    │       (Port 5002)            │
    │                             │    │                              │
    │  • Validate transaction     │    │  • Consume processed events  │
    │  • Apply business rules     │    │  • Log audit trail           │
    │  • Update payment status    │    │  • Simulate email/SMS alert  │
    │  • Publish to processed     │    │                              │
    │    topic (idempotent)       │    └──────────────────────────────┘
    └─────────────────────────────┘
                   │
    ┌──────────────▼──────────────┐
    │        PostgreSQL           │
    │                             │
    │  • transactions table       │
    │  • outbox_messages table    │
    │  • payment_audits table     │
    └─────────────────────────────┘
```

---

## Key Patterns Demonstrated

| Pattern | Where | Why It Matters |
|---|---|---|
| **Transactional Outbox** | FinFlow.API | Guarantees events are published even if Kafka is temporarily unavailable — no dual-write failures |
| **Idempotent Consumers** | PaymentService | Processes each event exactly once using deduplication keys — safe for at-least-once delivery |
| **Event-Driven Architecture** | All services | Services are fully decoupled — PaymentService has no HTTP dependency on the API |
| **Real-Time Push** | SignalR Hub | Dashboard receives live updates without polling |
| **CQRS-lite** | API layer | Separate read (query) and write (command) paths |

---

## Tech Stack

**Backend**
- .NET 6 / ASP.NET Core — REST APIs and SignalR hub
- Confluent.Kafka — producer/consumer with at-least-once delivery
- Entity Framework Core 6 — ORM with PostgreSQL provider
- MassTransit — message bus abstraction (used in NotificationService)
- Serilog — structured logging
- xUnit + Moq — unit and integration tests

**Frontend**
- React 18 with Hooks
- @microsoft/signalr — WebSocket client for live dashboard
- Recharts — transaction volume charts
- Axios — HTTP client

**Infrastructure**
- Docker Compose — single command local setup
- Apache Kafka + Zookeeper
- PostgreSQL 15
- pgAdmin (optional, for DB inspection)

---

## Getting Started

### Prerequisites
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (includes Docker Compose)
- [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
- [Node.js 18+](https://nodejs.org/)

### 1. Clone and configure

```bash
git clone https://github.com/sriputta-dev/finflow.git
cd finflow
cp .env.example .env
```

### 2. Start infrastructure (Kafka + PostgreSQL)

```bash
docker-compose up -d zookeeper kafka postgres
```

Wait ~15 seconds for Kafka to be ready, then:

```bash
docker-compose up -d  # starts all services
```

### 3. Run the .NET services

```bash
# Terminal 1 — API
cd src/FinFlow.API
dotnet run

# Terminal 2 — Payment Service
cd src/FinFlow.PaymentService
dotnet run

# Terminal 3 — Notification Service
cd src/FinFlow.NotificationService
dotnet run
```

### 4. Start the React dashboard

```bash
cd frontend
npm install
npm start
```

Open **http://localhost:3000** to see the live dashboard.

### 5. Submit a test payment

```bash
curl -X POST http://localhost:5000/api/transactions \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 1500.00,
    "currency": "USD",
    "senderId": "acc-001",
    "receiverId": "acc-002",
    "description": "Invoice payment"
  }'
```

Watch the dashboard update in real time.

---

## Project Structure

```
finflow/
├── src/
│   ├── FinFlow.API/                  # Entry point — REST API + SignalR hub
│   │   ├── Controllers/
│   │   │   └── TransactionsController.cs
│   │   ├── Hubs/
│   │   │   └── DashboardHub.cs       # SignalR real-time push
│   │   ├── Services/
│   │   │   ├── TransactionService.cs
│   │   │   └── OutboxPublisher.cs    # Background outbox processor
│   │   ├── Data/
│   │   │   └── FinFlowDbContext.cs
│   │   └── Program.cs
│   │
│   ├── FinFlow.PaymentService/        # Kafka consumer — validates & processes
│   │   ├── Consumers/
│   │   │   └── TransactionConsumer.cs  # Idempotent processing
│   │   ├── Models/
│   │   └── Program.cs
│   │
│   ├── FinFlow.NotificationService/   # Kafka consumer — audit & alerts
│   │   ├── Consumers/
│   │   │   └── PaymentProcessedConsumer.cs
│   │   └── Program.cs
│   │
│   └── FinFlow.Shared/                # Shared event contracts
│       └── Events/
│           ├── TransactionCreatedEvent.cs
│           └── PaymentProcessedEvent.cs
│
├── frontend/                          # React dashboard
│   └── src/
│       ├── components/
│       │   ├── TransactionFeed.jsx    # Live feed via SignalR
│       │   └── MetricsChart.jsx       # Recharts volume graph
│       ├── hooks/
│       │   └── useSignalR.js
│       └── App.jsx
│
├── docker-compose.yml
├── .env.example
└── README.md
```

---

## Running Tests

```bash
cd src/FinFlow.API
dotnet test

cd src/FinFlow.PaymentService
dotnet test
```

---

## Environment Variables

See `.env.example` for all required configuration:

```env
KAFKA_BOOTSTRAP_SERVERS=localhost:9092
POSTGRES_CONNECTION=Host=localhost;Database=finflow;Username=finflow;Password=finflow123
SIGNALR_CORS_ORIGIN=http://localhost:3000
```

---

## Why This Architecture?

In production payment systems, two problems are common:

1. **Lost events** — if you write to DB and then publish to Kafka, and Kafka fails between those two steps, the event is lost forever. The **Outbox Pattern** solves this by writing both the transaction and the outbox message in a single DB transaction, then publishing asynchronously.

2. **Duplicate processing** — Kafka guarantees at-least-once delivery, meaning consumers may receive the same event twice. The PaymentService uses a **deduplication key** stored in PostgreSQL to ensure each transaction is processed exactly once.

These two patterns together give you **exactly-once semantics** without requiring Kafka's transactional API.

---

## License

MIT
