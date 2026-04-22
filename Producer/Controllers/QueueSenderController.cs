using CommonResources;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Producer.Data;

namespace Producer.Controllers;

[ApiController]
[Route("[controller]")]
public class QueueSenderController : ControllerBase
{
    private readonly IBus _bus;
    private readonly IRequestClient<TransferData> _client;
    private readonly ILogger<QueueSenderController> _logger;
    private readonly IConfiguration _configuration;

    public QueueSenderController(
        IBus bus,
        IRequestClient<TransferData> client,
        ILogger<QueueSenderController> logger,
        IConfiguration configuration)
    {
        _bus = bus;
        _client = client;
        _logger = logger;
        _configuration = configuration;
    }

    // ──────────────────────────────────────────────
    // Existing patterns
    // ──────────────────────────────────────────────

    [HttpPost("send-command")]
    public async Task<IActionResult> SendCommand()
    {
        var account = new Account { Name = "David Bytyqi", Deposit = 500 };
        var url = new Uri("rabbitmq://localhost/send-command");
        var endpoint = await _bus.GetSendEndpoint(url);
        await endpoint.Send(account);
        return Ok("Command sent successfully");
    }

    [HttpPost("publish-event")]
    public async Task<IActionResult> PublishEvent()
    {
        await _bus.Publish(new Client { Name = "David Bytyqi", Pin = 123456 });
        return Ok("Event published successfully");
    }

    [HttpPost("request-response")]
    public async Task<IActionResult> RequestResponse()
    {
        var request = _client.Create(new TransferData { Type = "Test", Amount = 25 });
        var response = await request.GetResponse<CurrentBalance>();
        return Ok(response);
    }

    // ──────────────────────────────────────────────
    // Pattern 1: Retry Policy
    // ──────────────────────────────────────────────

    [HttpPost("retry-demo")]
    public async Task<IActionResult> RetryDemo()
    {
        var rabbitHost = _configuration["RabbitMQ:Host"]!;
        var endpointUri = new Uri($"{rabbitHost}/retry-demo");

        _logger.LogInformation(">>> RETRY | Sending RetryCommand — watch Consumer logs to see retries");

        var endpoint = await _bus.GetSendEndpoint(endpointUri);
        await endpoint.Send(new RetryCommand { JobName = "BackupJob" });

        return Ok("RetryCommand sent — watch Consumer logs for retry attempts (2s → 4s → 6s delays)");
    }

    // ──────────────────────────────────────────────
    // Pattern 2: Circuit Breaker
    // ──────────────────────────────────────────────

    [HttpPost("circuit-breaker-demo")]
    public async Task<IActionResult> CircuitBreakerDemo()
    {
        var rabbitHost = _configuration["RabbitMQ:Host"]!;
        var endpointUri = new Uri($"{rabbitHost}/circuit-breaker-demo");

        _logger.LogInformation(">>> CIRCUIT BREAKER | Sending CircuitBreakerCommand — call this 6+ times rapidly");

        var endpoint = await _bus.GetSendEndpoint(endpointUri);
        await endpoint.Send(new CircuitBreakerCommand { ServiceName = "PaymentGateway" });

        return Ok("CircuitBreakerCommand sent — call this endpoint 6+ times quickly to trip the breaker");
    }

    // ──────────────────────────────────────────────
    // Pattern 3: Outbox
    // ──────────────────────────────────────────────

    [HttpPost("outbox-demo")]
    public async Task<IActionResult> OutboxDemo([FromServices] OutboxDbContext db)
    {
        _logger.LogInformation(">>> OUTBOX | Publishing OutboxMessage — it will be saved to SQLite first, then sent to RabbitMQ");

        await _bus.Publish(new OutboxMessage
        {
            Payload = $"Hello from Outbox at {DateTime.UtcNow:HH:mm:ss}"
        });

        // Count pending messages in the outbox to show students the DB save happened
        var pendingCount = await db.Set<MassTransit.EntityFrameworkCoreIntegration.OutboxMessage>()
            .CountAsync();

        _logger.LogInformation(">>> OUTBOX | Outbox currently has {Count} pending message(s) waiting to be delivered", pendingCount);

        return Ok(new
        {
            Message = "OutboxMessage published — saved to SQLite outbox before RabbitMQ delivery",
            PendingInOutbox = pendingCount
        });
    }

    // ──────────────────────────────────────────────
    // Pattern 4: Saga
    // ──────────────────────────────────────────────

    [HttpPost("saga/place-order")]
    public async Task<IActionResult> PlaceOrder()
    {
        var orderId = Guid.NewGuid();

        _logger.LogInformation(">>> SAGA | Placing order {OrderId}", orderId);

        await _bus.Publish(new OrderPlaced { OrderId = orderId, ProductName = "Laptop" });

        return Ok(new
        {
            Message = "OrderPlaced published — copy the OrderId and use it for the next steps",
            OrderId = orderId
        });
    }

    [HttpPost("saga/approve-order/{orderId:guid}")]
    public async Task<IActionResult> ApproveOrder(Guid orderId)
    {
        _logger.LogInformation(">>> SAGA | Approving order {OrderId}", orderId);

        await _bus.Publish(new OrderApproved { OrderId = orderId });

        return Ok(new { Message = "OrderApproved published", OrderId = orderId });
    }

    [HttpPost("saga/complete-order/{orderId:guid}")]
    public async Task<IActionResult> CompleteOrder(Guid orderId)
    {
        _logger.LogInformation(">>> SAGA | Completing order {OrderId}", orderId);

        await _bus.Publish(new OrderCompleted { OrderId = orderId });

        return Ok(new { Message = "OrderCompleted published — check Consumer logs to see saga finish", OrderId = orderId });
    }
}
