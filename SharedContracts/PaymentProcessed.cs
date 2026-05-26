namespace SharedContracts;

public record PaymentProcessed
{
    public Guid OrderId { get; init; }
}
