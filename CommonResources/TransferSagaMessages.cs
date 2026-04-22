namespace CommonResources;

/// <summary>
/// Событие — запуск саги (отправляется из Producer)
/// </summary>
public record TransferRequested
{
    public Guid CorrelationId { get; init; }
    public string FromAccount { get; init; } = string.Empty;
    public string ToAccount { get; init; } = string.Empty;
    public decimal Amount { get; init; }
}

/// <summary>
/// Событие — перевод успешно обработан (отправляется из TransferDataRequestConsumer)
/// </summary>
public record TransferCompleted
{
    public Guid CorrelationId { get; init; }
    public decimal FinalBalance { get; init; }
}

/// <summary>
/// Событие — перевод отклонён (например, недостаточно средств)
/// </summary>
public record TransferFailed
{
    public Guid CorrelationId { get; init; }
    public string Reason { get; init; } = string.Empty;
}
