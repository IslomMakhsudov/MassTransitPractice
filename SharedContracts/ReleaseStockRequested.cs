namespace SharedContracts;

public record ReleaseStockRequested
{
    public Guid OrderId { get; init; }
}
