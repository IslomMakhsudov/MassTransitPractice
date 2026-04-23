namespace SharedContracts;

public record ProcessPaymentRequested
{
    public Guid OrderId { get; init; }
    public decimal Amount { get; init; }
}
