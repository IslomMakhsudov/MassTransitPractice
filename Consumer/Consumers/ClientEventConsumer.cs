using CommonResources;

using MassTransit;

namespace Consumer.Consumers;

public class ClientEventConsumer : IConsumer<Client>
{
    private readonly ILogger<ClientEventConsumer> _logger;

    public ClientEventConsumer(ILogger<ClientEventConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<Client> context)
    {
        _logger.LogInformation("Received Client event: Name={Name}, Pin={Pin}",
            context.Message.Name, context.Message.Pin);

        return Task.CompletedTask;
    }
}
