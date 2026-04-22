using CommonResources;

using MassTransit;

namespace Consumer.Consumers;

/// <summary>
/// Демонстрирует CIRCUIT BREAKER.
/// Если подряд приходит несколько сообщений с Pin=0 — цепочка ошибок "открывает" circuit breaker,
/// и последующие сообщения будут отклоняться немедленно без попытки обработки.
/// Настройка circuit breaker находится в Program.cs.
/// </summary>
public class ClientEventConsumer : IConsumer<Client>
{
    private readonly ILogger<ClientEventConsumer> _logger;

    public ClientEventConsumer(ILogger<ClientEventConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<Client> context)
    {
        _logger.LogInformation(
            "[CIRCUIT BREAKER DEMO] Received Client event: Name={Name}, Pin={Pin}",
            context.Message.Name, context.Message.Pin);

        // Симуляция: Pin=0 считается "недоступным сервисом" → бросаем исключение
        // После нескольких ошибок подряд circuit breaker "открывается"
        if (context.Message.Pin == 0)
        {
            _logger.LogWarning("[CIRCUIT BREAKER DEMO] Pin is 0 — simulating downstream service failure!");
            throw new Exception("Downstream service unavailable (simulated).");
        }

        _logger.LogInformation("[CIRCUIT BREAKER DEMO] Client event processed successfully.");
        return Task.CompletedTask;
    }
}
