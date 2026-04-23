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
