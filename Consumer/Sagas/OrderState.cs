using MassTransit;

namespace Consumer.Sagas;

public class OrderState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public DateTime PlacedAt { get; set; }
}
