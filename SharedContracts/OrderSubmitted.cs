namespace SharedContracts;

public record OrderSubmitted
{
    public Guid OrderId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal Amount { get; init; }
    public string CustomerEmail { get; init; } = string.Empty;
}
