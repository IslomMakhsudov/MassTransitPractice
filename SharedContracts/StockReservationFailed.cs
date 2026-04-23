namespace SharedContracts;

public record StockReservationFailed
{
    public Guid OrderId { get; init; }
    public string Reason { get; init; } = string.Empty;
}
