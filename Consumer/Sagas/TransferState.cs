using MassTransit;

namespace Consumer.Sagas;

/// <summary>
/// Состояние саги в базе данных (или in-memory репозитории).
/// Каждый экземпляр соответствует одному денежному переводу.
/// </summary>
public class TransferState : SagaStateMachineInstance
{
    /// <summary>Уникальный идентификатор экземпляра саги (= CorrelationId из сообщений)</summary>
    public Guid CorrelationId { get; set; }

    /// <summary>Текущее состояние: Initial / Pending / Completed / Failed</summary>
    public string CurrentState { get; set; } = string.Empty;

    public string FromAccount { get; set; } = string.Empty;
    public string ToAccount { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    public decimal? FinalBalance { get; set; }
    public string? FailureReason { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
