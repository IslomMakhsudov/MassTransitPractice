using CommonResources;
using MassTransit;

namespace Consumer.Consumers;

public class OutboxMessageConsumer : IConsumer<OutboxMessage>
{
    private readonly ILogger<OutboxMessageConsumer> _logger;

    public OutboxMessageConsumer(ILogger<OutboxMessageConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<OutboxMessage> context)
    {
        _logger.LogInformation(">>> OUTBOX | Message received from RabbitMQ: '{Payload}'",
            context.Message.Payload);

        _logger.LogInformation(">>> OUTBOX | This message was saved to SQLite outbox first, then delivered here");

        return Task.CompletedTask;
    }
}
