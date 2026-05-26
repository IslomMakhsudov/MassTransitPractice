# MassTransit Store Saga — Design Spec
**Date:** 2026-04-23  
**Author:** Islom Makhsudov  
**Pattern:** Saga Orchestration with Compensating Transactions

---

## Overview

Upgrade the MassTransitPractice project from a single Consumer/Producer pattern into four separate microservices connected through a store order flow. The goal is to give students a realistic, runnable example of how MassTransit saga orchestration works — including failure handling and compensating transactions.

---

## Project Structure

Five .NET projects replace the current `Consumer` and `Producer` layout. `CommonResources` is renamed to `SharedContracts`.

```
MassTransitPractice.sln
├── SharedContracts/       ← message contracts only; referenced by all services
├── OrderService/          ← HTTP API + OrderStateMachine + SQLite persistence
├── InventoryService/      ← handles ReserveStock and ReleaseStock events
├── PaymentService/        ← handles ProcessPayment event
└── NotificationService/   ← handles SendNotification event
```

Each service runs independently and communicates exclusively through RabbitMQ via MassTransit. No direct service-to-service HTTP calls.

---

## Message Contracts (SharedContracts)

### Trigger event — published by OrderService HTTP API

| Message | Fields |
|---|---|
| `OrderSubmitted` | `OrderId`, `ProductName`, `Quantity`, `Amount`, `CustomerEmail` |

### Saga → InventoryService

| Message | Fields |
|---|---|
| `ReserveStockRequested` | `OrderId`, `ProductName`, `Quantity` |
| `ReleaseStockRequested` | `OrderId` *(compensating transaction)* |

### InventoryService → Saga

| Message | Fields |
|---|---|
| `StockReserved` | `OrderId` |
| `StockReservationFailed` | `OrderId`, `Reason` |
| `StockReleased` | `OrderId` |

### Saga → PaymentService

| Message | Fields |
|---|---|
| `ProcessPaymentRequested` | `OrderId`, `Amount` |

### PaymentService → Saga

| Message | Fields |
|---|---|
| `PaymentProcessed` | `OrderId` |
| `PaymentFailed` | `OrderId`, `Reason` |

### Saga → NotificationService

| Message | Fields |
|---|---|
| `SendNotificationRequested` | `OrderId`, `CustomerEmail`, `Subject`, `Body` |

### NotificationService → Saga

| Message | Fields |
|---|---|
| `NotificationSent` | `OrderId` |

---

## Saga States & Flow

The `OrderStateMachine` in `OrderService` manages the following states:

```
Initial
  │ OrderSubmitted → publish ReserveStockRequested
  ▼
ReservingStock
  ├── StockReservationFailed → publish SendNotificationRequested (failure)
  │     ▼
  │   Cancelling ──NotificationSent──► Cancelled (final)
  │
  └── StockReserved → publish ProcessPaymentRequested
        ▼
      ProcessingPayment
        ├── PaymentFailed → publish ReleaseStockRequested
        │     ▼
        │   ReleasingStock ──StockReleased──► Cancelling
        │
        └── PaymentProcessed → publish SendNotificationRequested (success)
              ▼
            SendingNotification ──NotificationSent──► Completed (final)
```

### State descriptions

| State | Meaning |
|---|---|
| `ReservingStock` | Waiting for InventoryService to confirm stock reservation |
| `ProcessingPayment` | Stock reserved; waiting for PaymentService to confirm charge |
| `ReleasingStock` | Payment failed; waiting for InventoryService to release the reservation |
| `Cancelling` | Sending failure notification to customer |
| `SendingNotification` | Sending success notification to customer |
| `Completed` | Order fulfilled — terminal state |
| `Cancelled` | Order failed — terminal state |

### Saga persistence

Saga state is persisted in **SQLite via Entity Framework** inside `OrderService`. This means:
- State survives service restarts
- Students can query `GET /orders/{orderId}` at any point to see the current state
- Demonstrates why persistent saga state matters in distributed systems

---

## Service Behavior

### OrderService (port 5000)

**HTTP endpoints:**
- `POST /orders` — accepts `{ productName, quantity, amount, customerEmail }`, publishes `OrderSubmitted`, returns `{ orderId }`
- `GET /orders/{orderId}` — reads saga state from SQLite, returns `{ orderId, currentState, productName, customerEmail, placedAt, completedAt, failureReason }`

**Infrastructure:**
- Hosts `OrderStateMachine` registered with MassTransit
- SQLite database (`orders.db`) with EF Core for saga state persistence
- Subscribes to all result events (`StockReserved`, `StockReservationFailed`, `StockReleased`, `PaymentProcessed`, `PaymentFailed`, `NotificationSent`) correlated by `OrderId`

### InventoryService (port 5001)

- Subscribes to `ReserveStockRequested` — simulates stock check, **randomly fails ~30%** of the time, publishes `StockReserved` or `StockReservationFailed`
- Subscribes to `ReleaseStockRequested` — always succeeds, publishes `StockReleased`
- No HTTP API — pure message consumer

### PaymentService (port 5002)

- Subscribes to `ProcessPaymentRequested` — simulates payment processing, **randomly fails ~30%** of the time, publishes `PaymentProcessed` or `PaymentFailed`
- No HTTP API — pure message consumer

### NotificationService (port 5003)

- Subscribes to `SendNotificationRequested` — logs the notification to console (simulates email), publishes `NotificationSent`
- No HTTP API — pure message consumer

**Note:** The ~30% random failure rate in InventoryService and PaymentService is intentional. Students can place multiple orders and observe different saga execution paths through the state machine without needing to configure anything.

---

## Data Flow Example (Happy Path)

```
Student → POST /orders
  → OrderService publishes OrderSubmitted
    → Saga starts, transitions to ReservingStock
      → publishes ReserveStockRequested
        → InventoryService handles it, publishes StockReserved
          → Saga transitions to ProcessingPayment
            → publishes ProcessPaymentRequested
              → PaymentService handles it, publishes PaymentProcessed
                → Saga transitions to SendingNotification
                  → publishes SendNotificationRequested
                    → NotificationService handles it, publishes NotificationSent
                      → Saga transitions to Completed (finalized)

Student → GET /orders/{orderId} → { currentState: "Completed" }
```

## Data Flow Example (Compensating Transaction Path)

```
... StockReserved received, saga in ProcessingPayment ...
  → PaymentFailed received
    → Saga transitions to ReleasingStock
      → publishes ReleaseStockRequested
        → InventoryService releases reservation, publishes StockReleased
          → Saga transitions to Cancelling
            → publishes SendNotificationRequested (failure message)
              → NotificationService publishes NotificationSent
                → Saga transitions to Cancelled (finalized)
```

---

## Technology Stack

| Concern | Choice |
|---|---|
| Framework | ASP.NET Core (.NET 10) |
| Message broker | RabbitMQ (existing Docker setup) |
| MassTransit version | Existing (latest stable) |
| Saga persistence | SQLite + EF Core (EF Core Saga Repository) |
| Saga type | `MassTransitStateMachine<OrderSagaState>` |

---

## What Students Learn

1. **Saga orchestration** — a central state machine coordinates multiple services
2. **Compensating transactions** — how to undo work when a later step fails
3. **Event-driven communication** — services are fully decoupled; they only know about events
4. **Saga state persistence** — why saving state to a database matters
5. **Failure as a first-class concern** — the system handles partial failures gracefully
