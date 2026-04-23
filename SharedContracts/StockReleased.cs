namespace SharedContracts;

public record StockReleased
{
    public Guid OrderId { get; init; }
}
