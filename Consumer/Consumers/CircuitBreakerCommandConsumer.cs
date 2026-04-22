using CommonResources;
using MassTransit;

namespace Consumer.Consumers;

public class CircuitBreakerCommandConsumer : IConsumer<CircuitBreakerCommand>
{
    private readonly ILogger<CircuitBreakerCommandConsumer> _logger;

    // Static counter — first 5 calls fail to trip the breaker, then it recovers
    private static int _callCount = 0;
    private const int FailUntilCall = 5;

    public CircuitBreakerCommandConsumer(ILogger<CircuitBreakerCommandConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<CircuitBreakerCommand> context)
    {
        _callCount++;

        _logger.LogWarning(">>> CIRCUIT BREAKER | Call #{CallCount} — service '{ServiceName}'",
            _callCount, context.Message.ServiceName);

        if (_callCount <= FailUntilCall)
        {
            _logger.LogError(">>> CIRCUIT BREAKER | Call #{CallCount} FAILED — external service is down!",
                _callCount);

            throw new Exception($"External service unavailable (call #{_callCount})");
        }

        _logger.LogInformation(">>> CIRCUIT BREAKER | Call #{CallCount} SUCCEEDED — service has recovered!",
            _callCount);

        return Task.CompletedTask;
    }
}
