using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Saga;
using SharedContracts;

namespace OrderService.Controllers;

[ApiController]
[Route("orders")]
public class OrdersController(IPublishEndpoint publishEndpoint, OrderSagaDbContext dbContext) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request)
    {
        var orderId = Guid.NewGuid();

        await publishEndpoint.Publish(new OrderSubmitted
        {
            OrderId       = orderId,
            ProductName   = request.ProductName,
            Quantity      = request.Quantity,
            Amount        = request.Amount,
            CustomerEmail = request.CustomerEmail
        });

        return Ok(new { orderId });
    }

    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> GetOrder(Guid orderId)
    {
        var saga = await dbContext.Set<OrderSagaState>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CorrelationId == orderId);

        if (saga is null)
            return NotFound();

        return Ok(new
        {
            orderId      = saga.CorrelationId,
            currentState = saga.CurrentState,
            saga.ProductName,
            saga.CustomerEmail,
            placedAt     = saga.PlacedAt,
            completedAt  = saga.CompletedAt,
            failureReason = saga.FailureReason
        });
    }
}

public record PlaceOrderRequest(string ProductName, int Quantity, decimal Amount, string CustomerEmail);
