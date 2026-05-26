namespace SharedContracts;

public record ReserveStockRequested
{
    public Guid OrderId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
}
