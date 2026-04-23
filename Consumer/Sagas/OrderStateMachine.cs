using CommonResources;
using MassTransit;

namespace Consumer.Sagas;

public class OrderStateMachine : MassTransitStateMachine<OrderState>
{
    public State Placed { get; private set; } = null!;
    public State Approved { get; private set; } = null!;
    public State Completed { get; private set; } = null!;
    public State Cancelled { get; private set; } = null!;

    public Event<OrderPlaced> OrderPlacedEvent { get; private set; } = null!;
    public Event<OrderApproved> OrderApprovedEvent { get; private set; } = null!;
    public Event<OrderCompleted> OrderCompletedEvent { get; private set; } = null!;
    public Event<OrderCancelled> OrderCancelledEvent { get; private set; } = null!;

    public OrderStateMachine(ILogger<OrderStateMachine> logger)
    {
        // Tell MassTransit which property stores the current state name
        InstanceState(x => x.CurrentState);

        // Correlate all events to the same saga instance via OrderId
        Event(() => OrderPlacedEvent,    x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => OrderApprovedEvent,  x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => OrderCompletedEvent, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => OrderCancelledEvent, x => x.CorrelateById(ctx => ctx.Message.OrderId));

        // Initial state: saga doesn't exist yet, waiting for OrderPlaced
        Initially(
            When(OrderPlacedEvent)
                .Then(ctx =>
                {
                    ctx.Saga.ProductName = ctx.Message.ProductName;
                    ctx.Saga.PlacedAt = DateTime.UtcNow;
                    logger.LogInformation(
                        ">>> SAGA | [{OrderId}] OrderPlaced for '{Product}' — transitioning to Placed",
                        ctx.Message.OrderId, ctx.Message.ProductName);
                })
                .TransitionTo(Placed)
        );

        // Placed state: waiting for OrderApproved or cancellation
        During(Placed,
            When(OrderApprovedEvent)
                .Then(ctx =>
                    logger.LogInformation(
                        ">>> SAGA | [{OrderId}] OrderApproved — transitioning to Approved",
                        ctx.Message.OrderId))
                .TransitionTo(Approved),

            When(OrderCancelledEvent)
                .Then(ctx =>
                {
                    ctx.Saga.CancellationReason = ctx.Message.Reason;
                    // Compensating action: order was only placed, so we just free the reservation
                    logger.LogWarning(
                        ">>> SAGA | [{OrderId}] COMPENSATION | Order cancelled while Placed — releasing stock reservation. Reason: '{Reason}'",
                        ctx.Message.OrderId, ctx.Message.Reason);
                })
                .TransitionTo(Cancelled)
                .Finalize()
        );

        // Approved state: waiting for OrderCompleted or cancellation
        During(Approved,
            When(OrderCompletedEvent)
                .Then(ctx =>
                    logger.LogInformation(
                        ">>> SAGA | [{OrderId}] OrderCompleted — saga finished! Product: '{Product}', placed at {PlacedAt}",
                        ctx.Message.OrderId, ctx.Saga.ProductName, ctx.Saga.PlacedAt))
                .TransitionTo(Completed)
                .Finalize(),

            When(OrderCancelledEvent)
                .Then(ctx =>
                {
                    ctx.Saga.CancellationReason = ctx.Message.Reason;
                    // Compensating action: payment was already charged — must refund
                    logger.LogWarning(
                        ">>> SAGA | [{OrderId}] COMPENSATION | Order cancelled while Approved — issuing refund for '{Product}'. Reason: '{Reason}'",
                        ctx.Message.OrderId, ctx.Saga.ProductName, ctx.Message.Reason);
                })
                .TransitionTo(Cancelled)
                .Finalize()
        );

        // Remove finalized sagas from in-memory repository to avoid memory leaks
        SetCompletedWhenFinalized();
    }
}
