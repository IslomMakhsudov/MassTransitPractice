using CommonResources;

using MassTransit;

namespace Consumer.Sagas;

/// <summary>
/// SAGA — оркестрирует процесс денежного перевода через состояния:
///
///   [Initial]
///      │  TransferRequested
///      ▼
///   [Pending]  ←── ждём завершения обработки
///      │  TransferCompleted        │  TransferFailed
///      ▼                           ▼
///   [Completed]               [Failed]
///
/// Наглядно показывает: как сага хранит состояние между несколькими сообщениями.
/// </summary>
public class TransferStateMachine : MassTransitStateMachine<TransferState>
{
    // ── Состояния ──────────────────────────────────────────────────────────
    public State Pending { get; private set; } = null!;
    public State Completed { get; private set; } = null!;
    public State Failed { get; private set; } = null!;

    // ── События (триггеры переходов) ────────────────────────────────────────
    public Event<TransferRequested> TransferRequested { get; private set; } = null!;
    public Event<TransferCompleted> TransferCompleted { get; private set; } = null!;
    public Event<TransferFailed> TransferFailed { get; private set; } = null!;

    public TransferStateMachine()
    {
        // Говорим саге, какое поле хранит текущее состояние
        InstanceState(x => x.CurrentState);

        // Связываем события с CorrelationId
        Event(() => TransferRequested, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => TransferCompleted, x => x.CorrelateById(m => m.Message.CorrelationId));
        Event(() => TransferFailed, x => x.CorrelateById(m => m.Message.CorrelationId));

        // ── Переходы ────────────────────────────────────────────────────────

        // Initial → Pending: получили запрос на перевод
        Initially(
            When(TransferRequested)
                .Then(ctx =>
                {
                    ctx.Saga.FromAccount = ctx.Message.FromAccount;
                    ctx.Saga.ToAccount = ctx.Message.ToAccount;
                    ctx.Saga.Amount = ctx.Message.Amount;
                    ctx.Saga.CreatedAt = DateTime.UtcNow;

                    ctx.GetPayload<ILogger<TransferStateMachine>>()
                       .LogInformation(
                           "[SAGA] Transfer started. CorrelationId={Id}, From={From}, To={To}, Amount={Amount}",
                           ctx.Saga.CorrelationId, ctx.Saga.FromAccount, ctx.Saga.ToAccount, ctx.Saga.Amount);
                })
                .TransitionTo(Pending)
        );

        // Pending → Completed: перевод прошёл
        During(Pending,
            When(TransferCompleted)
                .Then(ctx =>
                {
                    ctx.Saga.FinalBalance = ctx.Message.FinalBalance;
                    ctx.Saga.CompletedAt = DateTime.UtcNow;

                    ctx.GetPayload<ILogger<TransferStateMachine>>()
                       .LogInformation(
                           "[SAGA] Transfer COMPLETED. CorrelationId={Id}, FinalBalance={Balance}",
                           ctx.Saga.CorrelationId, ctx.Saga.FinalBalance);
                })
                .TransitionTo(Completed)
                .Finalize(),

            // Pending → Failed: перевод отклонён
            When(TransferFailed)
                .Then(ctx =>
                {
                    ctx.Saga.FailureReason = ctx.Message.Reason;
                    ctx.Saga.CompletedAt = DateTime.UtcNow;

                    ctx.GetPayload<ILogger<TransferStateMachine>>()
                       .LogWarning(
                           "[SAGA] Transfer FAILED. CorrelationId={Id}, Reason={Reason}",
                           ctx.Saga.CorrelationId, ctx.Saga.FailureReason);
                })
                .TransitionTo(Failed)
                .Finalize()
        );

        // Завершённые саги удаляем из репозитория
        SetCompletedWhenFinalized();
    }
}
