using MassTransit;
using SharedContracts;

namespace OrderService.Saga;

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

    public OrderStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => OrderSubmittedEvent,        x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => StockReservedEvent,         x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => StockReservationFailedEvent,x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => StockReleasedEvent,         x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => PaymentProcessedEvent,      x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => PaymentFailedEvent,         x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => NotificationSentEvent,      x => x.CorrelateById(ctx => ctx.Message.OrderId));

        Initially(
            When(OrderSubmittedEvent)
                .Then(ctx =>
                {
                    ctx.Saga.ProductName   = ctx.Message.ProductName;
                    ctx.Saga.CustomerEmail = ctx.Message.CustomerEmail;
                    ctx.Saga.Quantity      = ctx.Message.Quantity;
                    ctx.Saga.Amount        = ctx.Message.Amount;
                    ctx.Saga.PlacedAt      = DateTime.UtcNow;
                })
                .ThenAsync(ctx => ctx.Publish(new ReserveStockRequested
                {
                    OrderId     = ctx.Saga.CorrelationId,
                    ProductName = ctx.Saga.ProductName,
                    Quantity    = ctx.Saga.Quantity
                }))
                .TransitionTo(ReservingStock)
        );

        During(ReservingStock,
            When(StockReservedEvent)
                .ThenAsync(ctx => ctx.Publish(new ProcessPaymentRequested
                {
                    OrderId = ctx.Saga.CorrelationId,
                    Amount  = ctx.Saga.Amount
                }))
                .TransitionTo(ProcessingPayment),

            When(StockReservationFailedEvent)
                .Then(ctx => ctx.Saga.FailureReason = ctx.Message.Reason)
                .ThenAsync(ctx => ctx.Publish(new SendNotificationRequested
                {
                    OrderId       = ctx.Saga.CorrelationId,
                    CustomerEmail = ctx.Saga.CustomerEmail,
                    Subject       = "Order Failed — Stock Unavailable",
                    Body          = $"Sorry, your order could not be processed: {ctx.Saga.FailureReason}"
                }))
                .TransitionTo(Cancelling)
        );

        During(ProcessingPayment,
            When(PaymentProcessedEvent)
                .Then(ctx => ctx.Saga.CompletedAt = DateTime.UtcNow)
                .ThenAsync(ctx => ctx.Publish(new SendNotificationRequested
                {
                    OrderId       = ctx.Saga.CorrelationId,
                    CustomerEmail = ctx.Saga.CustomerEmail,
                    Subject       = "Order Confirmed",
                    Body          = $"Your order for {ctx.Saga.ProductName} (qty: {ctx.Saga.Quantity}) has been confirmed!"
                }))
                .TransitionTo(SendingNotification),

            When(PaymentFailedEvent)
                .Then(ctx => ctx.Saga.FailureReason = ctx.Message.Reason)
                .ThenAsync(ctx => ctx.Publish(new ReleaseStockRequested
                {
                    OrderId = ctx.Saga.CorrelationId
                }))
                .TransitionTo(ReleasingStock)
        );

        During(ReleasingStock,
            When(StockReleasedEvent)
                .ThenAsync(ctx => ctx.Publish(new SendNotificationRequested
                {
                    OrderId       = ctx.Saga.CorrelationId,
                    CustomerEmail = ctx.Saga.CustomerEmail,
                    Subject       = "Order Cancelled — Payment Failed",
                    Body          = $"Sorry, your order was cancelled: {ctx.Saga.FailureReason}"
                }))
                .TransitionTo(Cancelling)
        );

        During(Cancelling,
            When(NotificationSentEvent)
                .Then(ctx => ctx.Saga.CompletedAt = DateTime.UtcNow)
                .TransitionTo(Cancelled)
        );

        During(SendingNotification,
            When(NotificationSentEvent)
                .TransitionTo(Completed)
        );
    }
}
