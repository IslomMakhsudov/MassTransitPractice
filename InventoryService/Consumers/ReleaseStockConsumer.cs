using MassTransit;
using SharedContracts;

namespace InventoryService.Consumers;

public class ReleaseStockConsumer : IConsumer<ReleaseStockRequested>
{
    public async Task Consume(ConsumeContext<ReleaseStockRequested> context)
    {
        var message = context.Message;

        await context.Publish(new StockReleased
        {
            OrderId = message.OrderId
        });
    }
}
