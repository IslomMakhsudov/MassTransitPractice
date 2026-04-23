namespace SharedContracts;

public record NotificationSent
{
    public Guid OrderId { get; init; }
}
