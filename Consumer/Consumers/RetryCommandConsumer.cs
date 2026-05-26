using CommonResources;
using MassTransit;

namespace Consumer.Consumers;

public class RetryCommandConsumer : IConsumer<RetryCommand>
{
    private readonly ILogger<RetryCommandConsumer> _logger;

    // Static counter persists across retries because MassTransit creates a new instance per attempt
    private static int _attemptCount = 0;
    private const int SucceedOnAttempt = 3;

    public RetryCommandConsumer(ILogger<RetryCommandConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<RetryCommand> context)
    {
        _attemptCount++;

        _logger.LogWarning(">>> RETRY | Attempt #{Attempt} for job '{JobName}'",
            _attemptCount, context.Message.JobName);

        if (_attemptCount < SucceedOnAttempt)
        {
            _logger.LogError(">>> RETRY | Attempt #{Attempt} FAILED — throwing exception to trigger retry",
                _attemptCount);

            throw new Exception($"Simulated failure on attempt #{_attemptCount}");
        }

        _logger.LogInformation(">>> RETRY | Attempt #{Attempt} SUCCEEDED! Job '{JobName}' completed.",
            _attemptCount, context.Message.JobName);

        _attemptCount = 0; // reset for next demo run
        return Task.CompletedTask;
    }
}
