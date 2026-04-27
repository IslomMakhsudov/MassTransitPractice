using InventoryService.Simulation;

using MassTransit;

using SharedContracts;

namespace InventoryService.Consumers;

public class ReserveStockConsumer : IConsumer<ReserveStockRequested>
{
    private readonly IFailureSimulator _failureSimulator;

    public ReserveStockConsumer(IFailureSimulator failureSimulator)
    {
        _failureSimulator = failureSimulator;
    }

    public async Task Consume(ConsumeContext<ReserveStockRequested> context)
    {
        var message = context.Message;

        if (_failureSimulator.ShouldFail())
        {
            await context.Publish(new StockReservationFailed
            {
                OrderId = message.OrderId,
                Reason = "Stock reservation failed due to simulated failure"
            });
        }
        else
        {
            await context.Publish(new StockReserved
            {
                OrderId = message.OrderId
            });
        }
    }
}
