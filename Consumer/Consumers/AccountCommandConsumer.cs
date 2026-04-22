using CommonResources;

using MassTransit;

namespace Consumer.Consumers;

/// <summary>
/// Демонстрирует RETRY POLICY.
/// Если депозит отрицательный — бросаем исключение, MassTransit повторит попытку.
/// Настройка retry находится в Program.cs (endpoint "send-command").
/// </summary>
public class AccountCommandConsumer : IConsumer<Account>
{
    private readonly ILogger<AccountCommandConsumer> _logger;

    // Счётчик попыток для наглядности в логах (static для простоты демо)
    private static int _attemptCount = 0;

    public AccountCommandConsumer(ILogger<AccountCommandConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<Account> context)
    {
        _attemptCount++;
        _logger.LogInformation(
            "[RETRY DEMO] Attempt #{Attempt} — Received Account command: Name={Name}, Deposit={Deposit}",
            _attemptCount, context.Message.Name, context.Message.Deposit);

        // Симуляция: если депозит отрицательный — бросаем ошибку → сработает retry
        if (context.Message.Deposit < 0)
        {
            _logger.LogWarning("[RETRY DEMO] Deposit is negative! Throwing exception to trigger retry...");
            throw new InvalidOperationException($"Deposit cannot be negative: {context.Message.Deposit}");
        }

        _logger.LogInformation("[RETRY DEMO] Account command processed successfully. Resetting attempt counter.");
        _attemptCount = 0;

        return Task.CompletedTask;
    }
}
