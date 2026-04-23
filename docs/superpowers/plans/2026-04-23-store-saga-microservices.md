# Store Saga Microservices Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the single Consumer/Producer pattern with four microservices (OrderService, InventoryService, PaymentService, NotificationService) coordinated by a SQLite-backed MassTransit saga with compensating transactions.

**Architecture:** OrderService hosts the `OrderStateMachine` with SQLite persistence (EF Core) and an HTTP API. InventoryService, PaymentService, and NotificationService are Worker services (no HTTP) that consume events and publish results back. All communication is via RabbitMQ publish/subscribe through MassTransit. The saga orchestrates the flow and executes a compensating transaction (`ReleaseStockRequested`) when payment fails after stock was already reserved.

**Tech Stack:** .NET 10, MassTransit 8.3.6, MassTransit.RabbitMQ 8.3.6, MassTransit.EntityFrameworkCore 8.3.6, EF Core SQLite 9.0.4, Swashbuckle 10.1.7, xUnit, MassTransit.Testing (bundled in MassTransit package)

---

## File Map

### SharedContracts (classlib — no dependencies)
- `SharedContracts/SharedContracts.csproj`
- `SharedContracts/OrderSubmitted.cs`
- `SharedContracts/ReserveStockRequested.cs`
- `SharedContracts/StockReserved.cs`
- `SharedContracts/StockReservationFailed.cs`
- `SharedContracts/ReleaseStockRequested.cs`
- `SharedContracts/StockReleased.cs`
- `SharedContracts/ProcessPaymentRequested.cs`
- `SharedContracts/PaymentProcessed.cs`
- `SharedContracts/PaymentFailed.cs`
- `SharedContracts/SendNotificationRequested.cs`
- `SharedContracts/NotificationSent.cs`

### InventoryService (Worker)
- `InventoryService/InventoryService.csproj`
- `InventoryService/Program.cs`
- `InventoryService/Simulation/IFailureSimulator.cs`
- `InventoryService/Simulation/RandomFailureSimulator.cs`
- `InventoryService/Consumers/ReserveStockConsumer.cs`
- `InventoryService/Consumers/ReleaseStockConsumer.cs`
- `InventoryService/appsettings.json`

### InventoryService.Tests (xunit)
- `InventoryService.Tests/InventoryService.Tests.csproj`
- `InventoryService.Tests/ReserveStockConsumerTests.cs`
- `InventoryService.Tests/ReleaseStockConsumerTests.cs`

### PaymentService (Worker)
- `PaymentService/PaymentService.csproj`
- `PaymentService/Program.cs`
- `PaymentService/Simulation/IFailureSimulator.cs`
- `PaymentService/Simulation/RandomFailureSimulator.cs`
- `PaymentService/Consumers/ProcessPaymentConsumer.cs`
- `PaymentService/appsettings.json`

### PaymentService.Tests (xunit)
- `PaymentService.Tests/PaymentService.Tests.csproj`
- `PaymentService.Tests/ProcessPaymentConsumerTests.cs`

### NotificationService (Worker)
- `NotificationService/NotificationService.csproj`
- `NotificationService/Program.cs`
- `NotificationService/Consumers/SendNotificationConsumer.cs`
- `NotificationService/appsettings.json`

### NotificationService.Tests (xunit)
- `NotificationService.Tests/NotificationService.Tests.csproj`
- `NotificationService.Tests/SendNotificationConsumerTests.cs`

### OrderService (WebAPI)
- `OrderService/OrderService.csproj`
- `OrderService/Program.cs`
- `OrderService/Sagas/OrderSagaState.cs`
- `OrderService/Sagas/OrderStateMachine.cs`
- `OrderService/Data/OrderSagaStateMap.cs`
- `OrderService/Data/OrderSagaDbContext.cs`
- `OrderService/Controllers/OrdersController.cs`
- `OrderService/appsettings.json`

### OrderService.Tests (xunit)
- `OrderService.Tests/OrderService.Tests.csproj`
- `OrderService.Tests/OrderStateMachineTests.cs`

---

## Task 1: SharedContracts — all message contracts

**Files:**
- Create: `SharedContracts/SharedContracts.csproj`
- Create: `SharedContracts/OrderSubmitted.cs` (and all 11 message files)

- [ ] **Step 1: Scaffold the classlib project**

```bash
cd C:/repos/MassTransitPractice
dotnet new classlib -n SharedContracts -f net10.0
dotnet sln add SharedContracts/SharedContracts.csproj
rm SharedContracts/Class1.cs
```

Expected: `SharedContracts/SharedContracts.csproj` created and added to solution.

- [ ] **Step 2: Write OrderSubmitted**

Create `SharedContracts/OrderSubmitted.cs`:
```csharp
namespace SharedContracts;

public record OrderSubmitted
{
    public Guid OrderId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal Amount { get; init; }
    public string CustomerEmail { get; init; } = string.Empty;
}
```

- [ ] **Step 3: Write Inventory contracts**

Create `SharedContracts/ReserveStockRequested.cs`:
```csharp
namespace SharedContracts;

public record ReserveStockRequested
{
    public Guid OrderId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
}
```

Create `SharedContracts/StockReserved.cs`:
```csharp
namespace SharedContracts;

public record StockReserved
{
    public Guid OrderId { get; init; }
}
```

Create `SharedContracts/StockReservationFailed.cs`:
```csharp
namespace SharedContracts;

public record StockReservationFailed
{
    public Guid OrderId { get; init; }
    public string Reason { get; init; } = string.Empty;
}
```

Create `SharedContracts/ReleaseStockRequested.cs`:
```csharp
namespace SharedContracts;

public record ReleaseStockRequested
{
    public Guid OrderId { get; init; }
}
```

Create `SharedContracts/StockReleased.cs`:
```csharp
namespace SharedContracts;

public record StockReleased
{
    public Guid OrderId { get; init; }
}
```

- [ ] **Step 4: Write Payment contracts**

Create `SharedContracts/ProcessPaymentRequested.cs`:
```csharp
namespace SharedContracts;

public record ProcessPaymentRequested
{
    public Guid OrderId { get; init; }
    public decimal Amount { get; init; }
}
```

Create `SharedContracts/PaymentProcessed.cs`:
```csharp
namespace SharedContracts;

public record PaymentProcessed
{
    public Guid OrderId { get; init; }
}
```

Create `SharedContracts/PaymentFailed.cs`:
```csharp
namespace SharedContracts;

public record PaymentFailed
{
    public Guid OrderId { get; init; }
    public string Reason { get; init; } = string.Empty;
}
```

- [ ] **Step 5: Write Notification contracts**

Create `SharedContracts/SendNotificationRequested.cs`:
```csharp
namespace SharedContracts;

public record SendNotificationRequested
{
    public Guid OrderId { get; init; }
    public string CustomerEmail { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
}
```

Create `SharedContracts/NotificationSent.cs`:
```csharp
namespace SharedContracts;

public record NotificationSent
{
    public Guid OrderId { get; init; }
}
```

- [ ] **Step 6: Build to verify**

```bash
dotnet build SharedContracts/SharedContracts.csproj
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 7: Commit**

```bash
git add SharedContracts/
git commit -m "feat: add SharedContracts with all message contracts"
```

---

## Task 2: InventoryService — stock reservation consumers

**Files:**
- Create: `InventoryService/InventoryService.csproj`
- Create: `InventoryService/Simulation/IFailureSimulator.cs`
- Create: `InventoryService/Simulation/RandomFailureSimulator.cs`
- Create: `InventoryService/Consumers/ReserveStockConsumer.cs`
- Create: `InventoryService/Consumers/ReleaseStockConsumer.cs`
- Create: `InventoryService/Program.cs`
- Create: `InventoryService/appsettings.json`
- Create: `InventoryService.Tests/InventoryService.Tests.csproj`
- Create: `InventoryService.Tests/ReserveStockConsumerTests.cs`
- Create: `InventoryService.Tests/ReleaseStockConsumerTests.cs`

- [ ] **Step 1: Scaffold InventoryService Worker project**

```bash
dotnet new worker -n InventoryService -f net10.0
dotnet sln add InventoryService/InventoryService.csproj
cd InventoryService
dotnet add package MassTransit --version 8.3.6
dotnet add package MassTransit.RabbitMQ --version 8.3.6
dotnet add reference ../SharedContracts/SharedContracts.csproj
cd ..
rm InventoryService/Worker.cs
```

- [ ] **Step 2: Scaffold InventoryService.Tests project**

```bash
dotnet new xunit -n InventoryService.Tests -f net10.0
dotnet sln add InventoryService.Tests/InventoryService.Tests.csproj
cd InventoryService.Tests
dotnet add package MassTransit --version 8.3.6
dotnet add reference ../SharedContracts/SharedContracts.csproj
dotnet add reference ../InventoryService/InventoryService.csproj
cd ..
```

- [ ] **Step 3: Write IFailureSimulator**

Create `InventoryService/Simulation/IFailureSimulator.cs`:
```csharp
namespace InventoryService.Simulation;

public interface IFailureSimulator
{
    bool ShouldFail();
}
```

- [ ] **Step 4: Write RandomFailureSimulator**

Create `InventoryService/Simulation/RandomFailureSimulator.cs`:
```csharp
namespace InventoryService.Simulation;

public class RandomFailureSimulator : IFailureSimulator
{
    private readonly double _failureRate;

    public RandomFailureSimulator(double failureRate = 0.3)
        => _failureRate = failureRate;

    public bool ShouldFail() => Random.Shared.NextDouble() < _failureRate;
}
```

- [ ] **Step 5: Write failing tests for ReserveStockConsumer**

Create `InventoryService.Tests/ReserveStockConsumerTests.cs`:
```csharp
using InventoryService.Consumers;
using InventoryService.Simulation;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using SharedContracts;

namespace InventoryService.Tests;

public class ReserveStockConsumerTests
{
    [Fact]
    public async Task WhenStockSucceeds_PublishesStockReserved()
    {
        await using var provider = new ServiceCollection()
            .AddSingleton<IFailureSimulator>(new RandomFailureSimulator(failureRate: 0.0))
            .AddMassTransitTestHarness(x => x.AddConsumer<ReserveStockConsumer>())
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var orderId = Guid.NewGuid();
        await harness.Bus.Publish(new ReserveStockRequested
            { OrderId = orderId, ProductName = "Laptop", Quantity = 1 });

        Assert.True(await harness.Consumed.Any<ReserveStockRequested>());
        Assert.True(await harness.Published.Any<StockReserved>(x => x.Context.Message.OrderId == orderId));
        Assert.False(await harness.Published.Any<StockReservationFailed>());
    }

    [Fact]
    public async Task WhenStockFails_PublishesStockReservationFailed()
    {
        await using var provider = new ServiceCollection()
            .AddSingleton<IFailureSimulator>(new RandomFailureSimulator(failureRate: 1.0))
            .AddMassTransitTestHarness(x => x.AddConsumer<ReserveStockConsumer>())
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var orderId = Guid.NewGuid();
        await harness.Bus.Publish(new ReserveStockRequested
            { OrderId = orderId, ProductName = "Laptop", Quantity = 1 });

        Assert.True(await harness.Consumed.Any<ReserveStockRequested>());
        Assert.True(await harness.Published.Any<StockReservationFailed>(x => x.Context.Message.OrderId == orderId));
        Assert.False(await harness.Published.Any<StockReserved>());
    }
}
```

- [ ] **Step 6: Run tests to confirm they fail**

```bash
dotnet test InventoryService.Tests/InventoryService.Tests.csproj
```

Expected: FAIL — `ReserveStockConsumer` does not exist yet.

- [ ] **Step 7: Write ReserveStockConsumer**

Create `InventoryService/Consumers/ReserveStockConsumer.cs`:
```csharp
using InventoryService.Simulation;
using MassTransit;
using SharedContracts;

namespace InventoryService.Consumers;

public class ReserveStockConsumer : IConsumer<ReserveStockRequested>
{
    private readonly IFailureSimulator _simulator;
    private readonly ILogger<ReserveStockConsumer> _logger;

    public ReserveStockConsumer(IFailureSimulator simulator, ILogger<ReserveStockConsumer> logger)
    {
        _simulator = simulator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ReserveStockRequested> context)
    {
        var msg = context.Message;

        if (_simulator.ShouldFail())
        {
            _logger.LogWarning("INVENTORY | [{OrderId}] Stock reservation FAILED for '{Product}'",
                msg.OrderId, msg.ProductName);
            await context.Publish(new StockReservationFailed
                { OrderId = msg.OrderId, Reason = "Insufficient stock" });
        }
        else
        {
            _logger.LogInformation("INVENTORY | [{OrderId}] Stock RESERVED for '{Product}' x{Qty}",
                msg.OrderId, msg.ProductName, msg.Quantity);
            await context.Publish(new StockReserved { OrderId = msg.OrderId });
        }
    }
}
```

- [ ] **Step 8: Write failing test for ReleaseStockConsumer**

Create `InventoryService.Tests/ReleaseStockConsumerTests.cs`:
```csharp
using InventoryService.Consumers;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using SharedContracts;

namespace InventoryService.Tests;

public class ReleaseStockConsumerTests
{
    [Fact]
    public async Task Consume_AlwaysPublishesStockReleased()
    {
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x => x.AddConsumer<ReleaseStockConsumer>())
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var orderId = Guid.NewGuid();
        await harness.Bus.Publish(new ReleaseStockRequested { OrderId = orderId });

        Assert.True(await harness.Consumed.Any<ReleaseStockRequested>());
        Assert.True(await harness.Published.Any<StockReleased>(x => x.Context.Message.OrderId == orderId));
    }
}
```

- [ ] **Step 9: Write ReleaseStockConsumer**

Create `InventoryService/Consumers/ReleaseStockConsumer.cs`:
```csharp
using MassTransit;
using SharedContracts;

namespace InventoryService.Consumers;

public class ReleaseStockConsumer : IConsumer<ReleaseStockRequested>
{
    private readonly ILogger<ReleaseStockConsumer> _logger;

    public ReleaseStockConsumer(ILogger<ReleaseStockConsumer> logger) => _logger = logger;

    public async Task Consume(ConsumeContext<ReleaseStockRequested> context)
    {
        _logger.LogInformation("INVENTORY | [{OrderId}] Stock RELEASED (compensating transaction)",
            context.Message.OrderId);
        await context.Publish(new StockReleased { OrderId = context.Message.OrderId });
    }
}
```

- [ ] **Step 10: Run tests to confirm they pass**

```bash
dotnet test InventoryService.Tests/InventoryService.Tests.csproj
```

Expected: `Passed! - 3 tests`

- [ ] **Step 11: Write Program.cs**

Replace `InventoryService/Program.cs`:
```csharp
using InventoryService.Consumers;
using InventoryService.Simulation;
using MassTransit;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IFailureSimulator, RandomFailureSimulator>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ReserveStockConsumer>();
    x.AddConsumer<ReleaseStockConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(new Uri(builder.Configuration["RabbitMQ:Host"]!), h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"]!);
            h.Password(builder.Configuration["RabbitMQ:Password"]!);
        });

        cfg.ConfigureEndpoints(ctx);
    });
});

var host = builder.Build();
host.Run();
```

- [ ] **Step 12: Write appsettings.json**

Create `InventoryService/appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "RabbitMQ": {
    "Host": "rabbitmq://localhost",
    "Username": "guest",
    "Password": "guest"
  }
}
```

- [ ] **Step 13: Build to verify**

```bash
dotnet build InventoryService/InventoryService.csproj
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 14: Commit**

```bash
git add InventoryService/ InventoryService.Tests/
git commit -m "feat: add InventoryService with ReserveStock and ReleaseStock consumers"
```

---

## Task 3: PaymentService — payment consumer

**Files:**
- Create: `PaymentService/PaymentService.csproj`
- Create: `PaymentService/Simulation/IFailureSimulator.cs`
- Create: `PaymentService/Simulation/RandomFailureSimulator.cs`
- Create: `PaymentService/Consumers/ProcessPaymentConsumer.cs`
- Create: `PaymentService/Program.cs`
- Create: `PaymentService/appsettings.json`
- Create: `PaymentService.Tests/PaymentService.Tests.csproj`
- Create: `PaymentService.Tests/ProcessPaymentConsumerTests.cs`

- [ ] **Step 1: Scaffold PaymentService Worker project**

```bash
dotnet new worker -n PaymentService -f net10.0
dotnet sln add PaymentService/PaymentService.csproj
cd PaymentService
dotnet add package MassTransit --version 8.3.6
dotnet add package MassTransit.RabbitMQ --version 8.3.6
dotnet add reference ../SharedContracts/SharedContracts.csproj
cd ..
rm PaymentService/Worker.cs
```

- [ ] **Step 2: Scaffold PaymentService.Tests project**

```bash
dotnet new xunit -n PaymentService.Tests -f net10.0
dotnet sln add PaymentService.Tests/PaymentService.Tests.csproj
cd PaymentService.Tests
dotnet add package MassTransit --version 8.3.6
dotnet add reference ../SharedContracts/SharedContracts.csproj
dotnet add reference ../PaymentService/PaymentService.csproj
cd ..
```

- [ ] **Step 3: Write IFailureSimulator and RandomFailureSimulator**

Create `PaymentService/Simulation/IFailureSimulator.cs`:
```csharp
namespace PaymentService.Simulation;

public interface IFailureSimulator
{
    bool ShouldFail();
}
```

Create `PaymentService/Simulation/RandomFailureSimulator.cs`:
```csharp
namespace PaymentService.Simulation;

public class RandomFailureSimulator : IFailureSimulator
{
    private readonly double _failureRate;

    public RandomFailureSimulator(double failureRate = 0.3)
        => _failureRate = failureRate;

    public bool ShouldFail() => Random.Shared.NextDouble() < _failureRate;
}
```

- [ ] **Step 4: Write failing tests**

Create `PaymentService.Tests/ProcessPaymentConsumerTests.cs`:
```csharp
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using PaymentService.Consumers;
using PaymentService.Simulation;
using SharedContracts;

namespace PaymentService.Tests;

public class ProcessPaymentConsumerTests
{
    [Fact]
    public async Task WhenPaymentSucceeds_PublishesPaymentProcessed()
    {
        await using var provider = new ServiceCollection()
            .AddSingleton<IFailureSimulator>(new RandomFailureSimulator(failureRate: 0.0))
            .AddMassTransitTestHarness(x => x.AddConsumer<ProcessPaymentConsumer>())
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var orderId = Guid.NewGuid();
        await harness.Bus.Publish(new ProcessPaymentRequested { OrderId = orderId, Amount = 99.99m });

        Assert.True(await harness.Consumed.Any<ProcessPaymentRequested>());
        Assert.True(await harness.Published.Any<PaymentProcessed>(x => x.Context.Message.OrderId == orderId));
        Assert.False(await harness.Published.Any<PaymentFailed>());
    }

    [Fact]
    public async Task WhenPaymentFails_PublishesPaymentFailed()
    {
        await using var provider = new ServiceCollection()
            .AddSingleton<IFailureSimulator>(new RandomFailureSimulator(failureRate: 1.0))
            .AddMassTransitTestHarness(x => x.AddConsumer<ProcessPaymentConsumer>())
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var orderId = Guid.NewGuid();
        await harness.Bus.Publish(new ProcessPaymentRequested { OrderId = orderId, Amount = 99.99m });

        Assert.True(await harness.Consumed.Any<ProcessPaymentRequested>());
        Assert.True(await harness.Published.Any<PaymentFailed>(x => x.Context.Message.OrderId == orderId));
        Assert.False(await harness.Published.Any<PaymentProcessed>());
    }
}
```

- [ ] **Step 5: Run tests to confirm they fail**

```bash
dotnet test PaymentService.Tests/PaymentService.Tests.csproj
```

Expected: FAIL — `ProcessPaymentConsumer` does not exist yet.

- [ ] **Step 6: Write ProcessPaymentConsumer**

Create `PaymentService/Consumers/ProcessPaymentConsumer.cs`:
```csharp
using MassTransit;
using PaymentService.Simulation;
using SharedContracts;

namespace PaymentService.Consumers;

public class ProcessPaymentConsumer : IConsumer<ProcessPaymentRequested>
{
    private readonly IFailureSimulator _simulator;
    private readonly ILogger<ProcessPaymentConsumer> _logger;

    public ProcessPaymentConsumer(IFailureSimulator simulator, ILogger<ProcessPaymentConsumer> logger)
    {
        _simulator = simulator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProcessPaymentRequested> context)
    {
        var msg = context.Message;

        if (_simulator.ShouldFail())
        {
            _logger.LogWarning("PAYMENT | [{OrderId}] Payment FAILED for amount {Amount:C}",
                msg.OrderId, msg.Amount);
            await context.Publish(new PaymentFailed { OrderId = msg.OrderId, Reason = "Card declined" });
        }
        else
        {
            _logger.LogInformation("PAYMENT | [{OrderId}] Payment PROCESSED for amount {Amount:C}",
                msg.OrderId, msg.Amount);
            await context.Publish(new PaymentProcessed { OrderId = msg.OrderId });
        }
    }
}
```

- [ ] **Step 7: Run tests to confirm they pass**

```bash
dotnet test PaymentService.Tests/PaymentService.Tests.csproj
```

Expected: `Passed! - 2 tests`

- [ ] **Step 8: Write Program.cs**

Replace `PaymentService/Program.cs`:
```csharp
using MassTransit;
using PaymentService.Consumers;
using PaymentService.Simulation;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IFailureSimulator, RandomFailureSimulator>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ProcessPaymentConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(new Uri(builder.Configuration["RabbitMQ:Host"]!), h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"]!);
            h.Password(builder.Configuration["RabbitMQ:Password"]!);
        });

        cfg.ConfigureEndpoints(ctx);
    });
});

var host = builder.Build();
host.Run();
```

- [ ] **Step 9: Write appsettings.json**

Create `PaymentService/appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "RabbitMQ": {
    "Host": "rabbitmq://localhost",
    "Username": "guest",
    "Password": "guest"
  }
}
```

- [ ] **Step 10: Build to verify**

```bash
dotnet build PaymentService/PaymentService.csproj
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 11: Commit**

```bash
git add PaymentService/ PaymentService.Tests/
git commit -m "feat: add PaymentService with ProcessPayment consumer"
```

---

## Task 4: NotificationService — notification consumer

**Files:**
- Create: `NotificationService/NotificationService.csproj`
- Create: `NotificationService/Consumers/SendNotificationConsumer.cs`
- Create: `NotificationService/Program.cs`
- Create: `NotificationService/appsettings.json`
- Create: `NotificationService.Tests/NotificationService.Tests.csproj`
- Create: `NotificationService.Tests/SendNotificationConsumerTests.cs`

- [ ] **Step 1: Scaffold NotificationService Worker project**

```bash
dotnet new worker -n NotificationService -f net10.0
dotnet sln add NotificationService/NotificationService.csproj
cd NotificationService
dotnet add package MassTransit --version 8.3.6
dotnet add package MassTransit.RabbitMQ --version 8.3.6
dotnet add reference ../SharedContracts/SharedContracts.csproj
cd ..
rm NotificationService/Worker.cs
```

- [ ] **Step 2: Scaffold NotificationService.Tests project**

```bash
dotnet new xunit -n NotificationService.Tests -f net10.0
dotnet sln add NotificationService.Tests/NotificationService.Tests.csproj
cd NotificationService.Tests
dotnet add package MassTransit --version 8.3.6
dotnet add reference ../SharedContracts/SharedContracts.csproj
dotnet add reference ../NotificationService/NotificationService.csproj
cd ..
```

- [ ] **Step 3: Write failing test**

Create `NotificationService.Tests/SendNotificationConsumerTests.cs`:
```csharp
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Consumers;
using SharedContracts;

namespace NotificationService.Tests;

public class SendNotificationConsumerTests
{
    [Fact]
    public async Task Consume_AlwaysPublishesNotificationSent()
    {
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x => x.AddConsumer<SendNotificationConsumer>())
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var orderId = Guid.NewGuid();
        await harness.Bus.Publish(new SendNotificationRequested
        {
            OrderId = orderId,
            CustomerEmail = "student@example.com",
            Subject = "Order Confirmed",
            Body = "Your order has been placed!"
        });

        Assert.True(await harness.Consumed.Any<SendNotificationRequested>());
        Assert.True(await harness.Published.Any<NotificationSent>(x => x.Context.Message.OrderId == orderId));
    }
}
```

- [ ] **Step 4: Run test to confirm it fails**

```bash
dotnet test NotificationService.Tests/NotificationService.Tests.csproj
```

Expected: FAIL — `SendNotificationConsumer` does not exist yet.

- [ ] **Step 5: Write SendNotificationConsumer**

Create `NotificationService/Consumers/SendNotificationConsumer.cs`:
```csharp
using MassTransit;
using SharedContracts;

namespace NotificationService.Consumers;

public class SendNotificationConsumer : IConsumer<SendNotificationRequested>
{
    private readonly ILogger<SendNotificationConsumer> _logger;

    public SendNotificationConsumer(ILogger<SendNotificationConsumer> logger) => _logger = logger;

    public async Task Consume(ConsumeContext<SendNotificationRequested> context)
    {
        var msg = context.Message;
        _logger.LogInformation(
            "NOTIFICATION | [{OrderId}] Email to {Email} — Subject: '{Subject}' | {Body}",
            msg.OrderId, msg.CustomerEmail, msg.Subject, msg.Body);

        await context.Publish(new NotificationSent { OrderId = msg.OrderId });
    }
}
```

- [ ] **Step 6: Run test to confirm it passes**

```bash
dotnet test NotificationService.Tests/NotificationService.Tests.csproj
```

Expected: `Passed! - 1 test`

- [ ] **Step 7: Write Program.cs**

Replace `NotificationService/Program.cs`:
```csharp
using MassTransit;
using NotificationService.Consumers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<SendNotificationConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(new Uri(builder.Configuration["RabbitMQ:Host"]!), h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"]!);
            h.Password(builder.Configuration["RabbitMQ:Password"]!);
        });

        cfg.ConfigureEndpoints(ctx);
    });
});

var host = builder.Build();
host.Run();
```

- [ ] **Step 8: Write appsettings.json**

Create `NotificationService/appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "RabbitMQ": {
    "Host": "rabbitmq://localhost",
    "Username": "guest",
    "Password": "guest"
  }
}
```

- [ ] **Step 9: Build to verify**

```bash
dotnet build NotificationService/NotificationService.csproj
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 10: Commit**

```bash
git add NotificationService/ NotificationService.Tests/
git commit -m "feat: add NotificationService with SendNotification consumer"
```

---

## Task 5: OrderService — project scaffold, saga state, DbContext

**Files:**
- Create: `OrderService/OrderService.csproj`
- Create: `OrderService/Sagas/OrderSagaState.cs`
- Create: `OrderService/Data/OrderSagaStateMap.cs`
- Create: `OrderService/Data/OrderSagaDbContext.cs`
- Create: `OrderService/appsettings.json`

- [ ] **Step 1: Scaffold OrderService WebAPI project**

```bash
dotnet new webapi -n OrderService -f net10.0
dotnet sln add OrderService/OrderService.csproj
cd OrderService
dotnet add package MassTransit --version 8.3.6
dotnet add package MassTransit.RabbitMQ --version 8.3.6
dotnet add package MassTransit.EntityFrameworkCore --version 8.3.6
dotnet add package Microsoft.EntityFrameworkCore.Sqlite --version 9.0.4
dotnet add package Swashbuckle.AspNetCore --version 10.1.7
dotnet add reference ../SharedContracts/SharedContracts.csproj
cd ..
```

- [ ] **Step 2: Remove generated boilerplate**

```bash
rm -f OrderService/WeatherForecast.cs
rm -f OrderService/Controllers/WeatherForecastController.cs
```

- [ ] **Step 3: Write OrderSagaState**

Create `OrderService/Sagas/OrderSagaState.cs`:
```csharp
using MassTransit;

namespace OrderService.Sagas;

public class OrderSagaState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Amount { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public DateTime PlacedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? FailureReason { get; set; }
}
```

- [ ] **Step 4: Write OrderSagaStateMap**

Create `OrderService/Data/OrderSagaStateMap.cs`:
```csharp
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderService.Sagas;

namespace OrderService.Data;

public class OrderSagaStateMap : SagaClassMap<OrderSagaState>
{
    protected override void Configure(EntityTypeBuilder<OrderSagaState> entity, ModelBuilder model)
    {
        entity.Property(x => x.CurrentState).HasMaxLength(64);
        entity.Property(x => x.ProductName).HasMaxLength(256);
        entity.Property(x => x.CustomerEmail).HasMaxLength(256);
        entity.Property(x => x.FailureReason).HasMaxLength(512);
    }
}
```

- [ ] **Step 5: Write OrderSagaDbContext**

Create `OrderService/Data/OrderSagaDbContext.cs`:
```csharp
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;

namespace OrderService.Data;

public class OrderSagaDbContext : SagaDbContext
{
    public OrderSagaDbContext(DbContextOptions<OrderSagaDbContext> options) : base(options) { }

    protected override IEnumerable<ISagaClassMap> Configurations
    {
        get { yield return new OrderSagaStateMap(); }
    }
}
```

- [ ] **Step 6: Write appsettings.json**

Replace `OrderService/appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "RabbitMQ": {
    "Host": "rabbitmq://localhost",
    "Username": "guest",
    "Password": "guest"
  }
}
```

- [ ] **Step 7: Build to verify**

```bash
dotnet build OrderService/OrderService.csproj
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 8: Commit**

```bash
git add OrderService/
git commit -m "feat: scaffold OrderService with saga state and EF Core DbContext"
```

---

## Task 6: OrderService — OrderStateMachine with tests

**Files:**
- Create: `OrderService/Sagas/OrderStateMachine.cs`
- Create: `OrderService.Tests/OrderService.Tests.csproj`
- Create: `OrderService.Tests/OrderStateMachineTests.cs`

- [ ] **Step 1: Scaffold OrderService.Tests project**

```bash
dotnet new xunit -n OrderService.Tests -f net10.0
dotnet sln add OrderService.Tests/OrderService.Tests.csproj
cd OrderService.Tests
dotnet add package MassTransit --version 8.3.6
dotnet add package MassTransit.EntityFrameworkCore --version 8.3.6
dotnet add package Microsoft.EntityFrameworkCore.Sqlite --version 9.0.4
dotnet add reference ../SharedContracts/SharedContracts.csproj
dotnet add reference ../OrderService/OrderService.csproj
cd ..
```

- [ ] **Step 2: Write failing saga tests**

Create `OrderService.Tests/OrderStateMachineTests.cs`:
```csharp
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Sagas;
using SharedContracts;

namespace OrderService.Tests;

public class OrderStateMachineTests
{
    private static ServiceProvider BuildProvider()
        => new ServiceCollection()
            .AddMassTransitTestHarness(x =>
                x.AddSagaStateMachine<OrderStateMachine, OrderSagaState>()
                    .InMemoryRepository())
            .BuildServiceProvider(true);

    private static OrderSubmitted SampleOrder(Guid orderId) => new()
    {
        OrderId = orderId, ProductName = "Laptop",
        Quantity = 1, Amount = 999m, CustomerEmail = "test@example.com"
    };

    [Fact]
    public async Task OrderSubmitted_TransitionsToReservingStock_AndPublishesReserveStockRequested()
    {
        await using var provider = BuildProvider();
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var orderId = Guid.NewGuid();
        await harness.Bus.Publish(SampleOrder(orderId));

        var sagaHarness = harness.GetSagaStateMachineHarness<OrderStateMachine, OrderSagaState>();
        Assert.True(await sagaHarness.Consumed.Any<OrderSubmitted>());
        Assert.True(await harness.Published.Any<ReserveStockRequested>(x => x.Context.Message.OrderId == orderId));

        var instance = sagaHarness.Sagas.Contains(orderId);
        Assert.NotNull(instance);
        Assert.Equal("ReservingStock", instance.CurrentState);
    }

    [Fact]
    public async Task StockReserved_TransitionsToProcessingPayment_AndPublishesProcessPaymentRequested()
    {
        await using var provider = BuildProvider();
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var orderId = Guid.NewGuid();
        var sagaHarness = harness.GetSagaStateMachineHarness<OrderStateMachine, OrderSagaState>();

        await harness.Bus.Publish(SampleOrder(orderId));
        Assert.True(await sagaHarness.Consumed.Any<OrderSubmitted>());

        await harness.Bus.Publish(new StockReserved { OrderId = orderId });
        Assert.True(await sagaHarness.Consumed.Any<StockReserved>());

        Assert.True(await harness.Published.Any<ProcessPaymentRequested>(x => x.Context.Message.OrderId == orderId));
        Assert.Equal("ProcessingPayment", sagaHarness.Sagas.Contains(orderId)!.CurrentState);
    }

    [Fact]
    public async Task StockReservationFailed_TransitionsToCancelling_AndPublishesSendNotificationRequested()
    {
        await using var provider = BuildProvider();
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var orderId = Guid.NewGuid();
        var sagaHarness = harness.GetSagaStateMachineHarness<OrderStateMachine, OrderSagaState>();

        await harness.Bus.Publish(SampleOrder(orderId));
        Assert.True(await sagaHarness.Consumed.Any<OrderSubmitted>());

        await harness.Bus.Publish(new StockReservationFailed { OrderId = orderId, Reason = "Out of stock" });
        Assert.True(await sagaHarness.Consumed.Any<StockReservationFailed>());

        Assert.True(await harness.Published.Any<SendNotificationRequested>(
            x => x.Context.Message.OrderId == orderId && x.Context.Message.Subject == "Order Failed"));
        Assert.Equal("Cancelling", sagaHarness.Sagas.Contains(orderId)!.CurrentState);
    }

    [Fact]
    public async Task PaymentProcessed_TransitionsToSendingNotification_AndPublishesConfirmationNotification()
    {
        await using var provider = BuildProvider();
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var orderId = Guid.NewGuid();
        var sagaHarness = harness.GetSagaStateMachineHarness<OrderStateMachine, OrderSagaState>();

        await harness.Bus.Publish(SampleOrder(orderId));
        Assert.True(await sagaHarness.Consumed.Any<OrderSubmitted>());

        await harness.Bus.Publish(new StockReserved { OrderId = orderId });
        Assert.True(await sagaHarness.Consumed.Any<StockReserved>());

        await harness.Bus.Publish(new PaymentProcessed { OrderId = orderId });
        Assert.True(await sagaHarness.Consumed.Any<PaymentProcessed>());

        Assert.True(await harness.Published.Any<SendNotificationRequested>(
            x => x.Context.Message.OrderId == orderId && x.Context.Message.Subject == "Order Confirmed"));
        Assert.Equal("SendingNotification", sagaHarness.Sagas.Contains(orderId)!.CurrentState);
    }

    [Fact]
    public async Task PaymentFailed_TransitionsToReleasingStock_AndPublishesReleaseStockRequested()
    {
        await using var provider = BuildProvider();
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var orderId = Guid.NewGuid();
        var sagaHarness = harness.GetSagaStateMachineHarness<OrderStateMachine, OrderSagaState>();

        await harness.Bus.Publish(SampleOrder(orderId));
        Assert.True(await sagaHarness.Consumed.Any<OrderSubmitted>());

        await harness.Bus.Publish(new StockReserved { OrderId = orderId });
        Assert.True(await sagaHarness.Consumed.Any<StockReserved>());

        await harness.Bus.Publish(new PaymentFailed { OrderId = orderId, Reason = "Card declined" });
        Assert.True(await sagaHarness.Consumed.Any<PaymentFailed>());

        Assert.True(await harness.Published.Any<ReleaseStockRequested>(x => x.Context.Message.OrderId == orderId));
        Assert.Equal("ReleasingStock", sagaHarness.Sagas.Contains(orderId)!.CurrentState);
    }

    [Fact]
    public async Task StockReleased_TransitionsToCancelling_AndPublishesCancelNotification()
    {
        await using var provider = BuildProvider();
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var orderId = Guid.NewGuid();
        var sagaHarness = harness.GetSagaStateMachineHarness<OrderStateMachine, OrderSagaState>();

        await harness.Bus.Publish(SampleOrder(orderId));
        Assert.True(await sagaHarness.Consumed.Any<OrderSubmitted>());

        await harness.Bus.Publish(new StockReserved { OrderId = orderId });
        Assert.True(await sagaHarness.Consumed.Any<StockReserved>());

        await harness.Bus.Publish(new PaymentFailed { OrderId = orderId, Reason = "Card declined" });
        Assert.True(await sagaHarness.Consumed.Any<PaymentFailed>());

        await harness.Bus.Publish(new StockReleased { OrderId = orderId });
        Assert.True(await sagaHarness.Consumed.Any<StockReleased>());

        Assert.True(await harness.Published.Any<SendNotificationRequested>(
            x => x.Context.Message.OrderId == orderId && x.Context.Message.Subject == "Order Cancelled"));
        Assert.Equal("Cancelling", sagaHarness.Sagas.Contains(orderId)!.CurrentState);
    }

    [Fact]
    public async Task NotificationSent_WhenInSendingNotification_TransitionsToCompleted()
    {
        await using var provider = BuildProvider();
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var orderId = Guid.NewGuid();
        var sagaHarness = harness.GetSagaStateMachineHarness<OrderStateMachine, OrderSagaState>();

        await harness.Bus.Publish(SampleOrder(orderId));
        Assert.True(await sagaHarness.Consumed.Any<OrderSubmitted>());

        await harness.Bus.Publish(new StockReserved { OrderId = orderId });
        Assert.True(await sagaHarness.Consumed.Any<StockReserved>());

        await harness.Bus.Publish(new PaymentProcessed { OrderId = orderId });
        Assert.True(await sagaHarness.Consumed.Any<PaymentProcessed>());

        await harness.Bus.Publish(new NotificationSent { OrderId = orderId });
        Assert.True(await sagaHarness.Consumed.Any<NotificationSent>());

        Assert.Equal("Completed", sagaHarness.Sagas.Contains(orderId)!.CurrentState);
    }

    [Fact]
    public async Task NotificationSent_WhenInCancelling_TransitionsToCancelled()
    {
        await using var provider = BuildProvider();
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var orderId = Guid.NewGuid();
        var sagaHarness = harness.GetSagaStateMachineHarness<OrderStateMachine, OrderSagaState>();

        await harness.Bus.Publish(SampleOrder(orderId));
        Assert.True(await sagaHarness.Consumed.Any<OrderSubmitted>());

        await harness.Bus.Publish(new StockReservationFailed { OrderId = orderId, Reason = "Out of stock" });
        Assert.True(await sagaHarness.Consumed.Any<StockReservationFailed>());

        await harness.Bus.Publish(new NotificationSent { OrderId = orderId });
        Assert.True(await sagaHarness.Consumed.Any<NotificationSent>());

        Assert.Equal("Cancelled", sagaHarness.Sagas.Contains(orderId)!.CurrentState);
    }
}
```

- [ ] **Step 3: Run tests to confirm they fail**

```bash
dotnet test OrderService.Tests/OrderService.Tests.csproj
```

Expected: FAIL — `OrderStateMachine` does not exist yet.

- [ ] **Step 4: Write OrderStateMachine**

Create `OrderService/Sagas/OrderStateMachine.cs`:
```csharp
using MassTransit;
using SharedContracts;

namespace OrderService.Sagas;

public class OrderStateMachine : MassTransitStateMachine<OrderSagaState>
{
    public State ReservingStock { get; private set; } = null!;
    public State ProcessingPayment { get; private set; } = null!;
    public State ReleasingStock { get; private set; } = null!;
    public State Cancelling { get; private set; } = null!;
    public State SendingNotification { get; private set; } = null!;
    public State Completed { get; private set; } = null!;
    public State Cancelled { get; private set; } = null!;

    public Event<OrderSubmitted> OrderSubmittedEvent { get; private set; } = null!;
    public Event<StockReserved> StockReservedEvent { get; private set; } = null!;
    public Event<StockReservationFailed> StockReservationFailedEvent { get; private set; } = null!;
    public Event<StockReleased> StockReleasedEvent { get; private set; } = null!;
    public Event<PaymentProcessed> PaymentProcessedEvent { get; private set; } = null!;
    public Event<PaymentFailed> PaymentFailedEvent { get; private set; } = null!;
    public Event<NotificationSent> NotificationSentEvent { get; private set; } = null!;

    public OrderStateMachine(ILogger<OrderStateMachine> logger)
    {
        InstanceState(x => x.CurrentState);

        Event(() => OrderSubmittedEvent,         x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => StockReservedEvent,          x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => StockReservationFailedEvent, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => StockReleasedEvent,          x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => PaymentProcessedEvent,       x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => PaymentFailedEvent,          x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => NotificationSentEvent,       x => x.CorrelateById(ctx => ctx.Message.OrderId));

        Initially(
            When(OrderSubmittedEvent)
                .Then(ctx =>
                {
                    ctx.Saga.ProductName   = ctx.Message.ProductName;
                    ctx.Saga.Quantity      = ctx.Message.Quantity;
                    ctx.Saga.Amount        = ctx.Message.Amount;
                    ctx.Saga.CustomerEmail = ctx.Message.CustomerEmail;
                    ctx.Saga.PlacedAt      = DateTime.UtcNow;
                    logger.LogInformation("SAGA | [{OrderId}] Order submitted — reserving stock", ctx.Message.OrderId);
                })
                .Publish(ctx => new ReserveStockRequested
                {
                    OrderId     = ctx.Saga.CorrelationId,
                    ProductName = ctx.Saga.ProductName,
                    Quantity    = ctx.Saga.Quantity
                })
                .TransitionTo(ReservingStock)
        );

        During(ReservingStock,
            When(StockReservedEvent)
                .Then(ctx => logger.LogInformation("SAGA | [{OrderId}] Stock reserved — processing payment", ctx.Message.OrderId))
                .Publish(ctx => new ProcessPaymentRequested
                {
                    OrderId = ctx.Saga.CorrelationId,
                    Amount  = ctx.Saga.Amount
                })
                .TransitionTo(ProcessingPayment),

            When(StockReservationFailedEvent)
                .Then(ctx =>
                {
                    ctx.Saga.FailureReason = ctx.Message.Reason;
                    logger.LogWarning("SAGA | [{OrderId}] Stock failed: {Reason} — cancelling", ctx.Message.OrderId, ctx.Message.Reason);
                })
                .Publish(ctx => new SendNotificationRequested
                {
                    OrderId       = ctx.Saga.CorrelationId,
                    CustomerEmail = ctx.Saga.CustomerEmail,
                    Subject       = "Order Failed",
                    Body          = $"Your order for {ctx.Saga.ProductName} could not be placed: {ctx.Saga.FailureReason}"
                })
                .TransitionTo(Cancelling)
        );

        During(ProcessingPayment,
            When(PaymentProcessedEvent)
                .Then(ctx => logger.LogInformation("SAGA | [{OrderId}] Payment processed — notifying customer", ctx.Message.OrderId))
                .Publish(ctx => new SendNotificationRequested
                {
                    OrderId       = ctx.Saga.CorrelationId,
                    CustomerEmail = ctx.Saga.CustomerEmail,
                    Subject       = "Order Confirmed",
                    Body          = $"Your order for {ctx.Saga.ProductName} has been confirmed and will be shipped soon!"
                })
                .TransitionTo(SendingNotification),

            When(PaymentFailedEvent)
                .Then(ctx =>
                {
                    ctx.Saga.FailureReason = ctx.Message.Reason;
                    logger.LogWarning("SAGA | [{OrderId}] Payment failed: {Reason} — releasing stock", ctx.Message.OrderId, ctx.Message.Reason);
                })
                .Publish(ctx => new ReleaseStockRequested { OrderId = ctx.Saga.CorrelationId })
                .TransitionTo(ReleasingStock)
        );

        During(ReleasingStock,
            When(StockReleasedEvent)
                .Then(ctx => logger.LogInformation("SAGA | [{OrderId}] Stock released — cancelling order", ctx.Message.OrderId))
                .Publish(ctx => new SendNotificationRequested
                {
                    OrderId       = ctx.Saga.CorrelationId,
                    CustomerEmail = ctx.Saga.CustomerEmail,
                    Subject       = "Order Cancelled",
                    Body          = $"Your order for {ctx.Saga.ProductName} was cancelled due to: {ctx.Saga.FailureReason}"
                })
                .TransitionTo(Cancelling)
        );

        During(SendingNotification,
            When(NotificationSentEvent)
                .Then(ctx =>
                {
                    ctx.Saga.CompletedAt = DateTime.UtcNow;
                    logger.LogInformation("SAGA | [{OrderId}] Order COMPLETED!", ctx.Message.OrderId);
                })
                .TransitionTo(Completed)
        );

        During(Cancelling,
            When(NotificationSentEvent)
                .Then(ctx =>
                {
                    ctx.Saga.CompletedAt = DateTime.UtcNow;
                    logger.LogInformation("SAGA | [{OrderId}] Order CANCELLED.", ctx.Message.OrderId);
                })
                .TransitionTo(Cancelled)
        );
    }
}
```

- [ ] **Step 5: Run tests to confirm they pass**

```bash
dotnet test OrderService.Tests/OrderService.Tests.csproj
```

Expected: `Passed! - 8 tests`

- [ ] **Step 6: Commit**

```bash
git add OrderService/Sagas/OrderStateMachine.cs OrderService.Tests/
git commit -m "feat: add OrderStateMachine with full compensating transaction flow"
```

---

## Task 7: OrderService — HTTP API and Program.cs wiring

**Files:**
- Create: `OrderService/Controllers/OrdersController.cs`
- Modify: `OrderService/Program.cs`

- [ ] **Step 1: Write OrdersController**

Create `OrderService/Controllers/OrdersController.cs`:
```csharp
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using OrderService.Data;
using OrderService.Sagas;
using SharedContracts;

namespace OrderService.Controllers;

[ApiController]
[Route("orders")]
public class OrdersController : ControllerBase
{
    private readonly IBus _bus;
    private readonly OrderSagaDbContext _db;

    public OrdersController(IBus bus, OrderSagaDbContext db)
    {
        _bus = bus;
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request)
    {
        var orderId = Guid.NewGuid();

        await _bus.Publish(new OrderSubmitted
        {
            OrderId       = orderId,
            ProductName   = request.ProductName,
            Quantity      = request.Quantity,
            Amount        = request.Amount,
            CustomerEmail = request.CustomerEmail
        });

        return Ok(new { OrderId = orderId });
    }

    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> GetOrder(Guid orderId)
    {
        var saga = await _db.Set<OrderSagaState>().FindAsync(orderId);
        if (saga is null)
            return NotFound(new { Message = $"Order {orderId} not found — it may still be initializing" });

        return Ok(new
        {
            saga.CorrelationId,
            saga.CurrentState,
            saga.ProductName,
            saga.Quantity,
            saga.Amount,
            saga.CustomerEmail,
            saga.PlacedAt,
            saga.CompletedAt,
            saga.FailureReason
        });
    }
}

public record PlaceOrderRequest(
    string ProductName,
    int Quantity,
    decimal Amount,
    string CustomerEmail);
```

- [ ] **Step 2: Write Program.cs**

Replace `OrderService/Program.cs`:
```csharp
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Sagas;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
    o.SwaggerDoc("v1", new() { Title = "Order Service", Version = "v1" }));

builder.Services.AddMassTransit(x =>
{
    x.AddSagaStateMachine<OrderStateMachine, OrderSagaState>()
        .EntityFrameworkRepository(r =>
        {
            r.ConcurrencyMode = ConcurrencyMode.Pessimistic;
            r.AddDbContext<DbContext, OrderSagaDbContext>((_, opts) =>
                opts.UseSqlite("Data Source=orders.db"));
            r.UseSqlite();
        });

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(new Uri(builder.Configuration["RabbitMQ:Host"]!), h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"]!);
            h.Password(builder.Configuration["RabbitMQ:Password"]!);
        });

        cfg.ConfigureEndpoints(ctx);
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderSagaDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(o =>
    {
        o.SwaggerEndpoint("/swagger/v1/swagger.json", "Order Service v1");
        o.RoutePrefix = "swagger";
    });
}

app.UseAuthorization();
app.MapControllers();
app.Run();
```

- [ ] **Step 3: Build to verify**

```bash
dotnet build OrderService/OrderService.csproj
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit**

```bash
git add OrderService/Controllers/ OrderService/Program.cs
git commit -m "feat: add OrdersController and wire OrderService with RabbitMQ and SQLite"
```

---

## Task 8: Remove old projects and verify full solution

**Files:**
- Remove: `Consumer/`, `Producer/`, `CommonResources/`

- [ ] **Step 1: Remove old projects from solution**

```bash
dotnet sln remove Consumer/Consumer.csproj
dotnet sln remove Producer/Producer.csproj
dotnet sln remove CommonResources/CommonResources.csproj
```

Expected: three `Removed project` confirmation lines.

- [ ] **Step 2: Delete old project folders**

```bash
rm -rf Consumer Producer CommonResources
```

- [ ] **Step 3: Verify solution contains only new projects**

```bash
dotnet sln list
```

Expected output contains exactly:
```
SharedContracts/SharedContracts.csproj
InventoryService/InventoryService.csproj
InventoryService.Tests/InventoryService.Tests.csproj
PaymentService/PaymentService.csproj
PaymentService.Tests/PaymentService.Tests.csproj
NotificationService/NotificationService.csproj
NotificationService.Tests/NotificationService.Tests.csproj
OrderService/OrderService.csproj
OrderService.Tests/OrderService.Tests.csproj
```

- [ ] **Step 4: Build entire solution**

```bash
dotnet build
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 5: Run all tests**

```bash
dotnet test
```

Expected: `Passed! - 14 tests` (3 InventoryService + 2 PaymentService + 1 NotificationService + 8 OrderService)

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "chore: remove old Consumer, Producer, CommonResources projects"
```

---

## Running the Full System

**Prerequisites:** RabbitMQ running locally.

```bash
docker run -d --hostname rabbit --name rabbit -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

**Start all four services in separate terminals:**

```bash
# Terminal 1 — OrderService (HTTP API + Saga)
dotnet run --project OrderService

# Terminal 2 — InventoryService
dotnet run --project InventoryService

# Terminal 3 — PaymentService
dotnet run --project PaymentService

# Terminal 4 — NotificationService
dotnet run --project NotificationService
```

**Place an order** via Swagger at `http://localhost:5000/swagger` → `POST /orders`:
```json
{
  "productName": "Laptop",
  "quantity": 1,
  "amount": 999.99,
  "customerEmail": "student@example.com"
}
```

**Poll the state** via `GET /orders/{orderId}` to watch the saga transition through states in real time.

**Trigger different paths:** Run `POST /orders` multiple times. Due to ~30% random failure rates in InventoryService and PaymentService, you will observe all saga paths — including the compensating transaction — without changing any code.
