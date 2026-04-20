using CommonResources;

using MassTransit;

namespace Consumer.Consumers;

public class AccountCommandConsumer : IConsumer<Account>
{
    private readonly ILogger<AccountCommandConsumer> _logger;

    public AccountCommandConsumer(ILogger<AccountCommandConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<Account> context)
    {
        _logger.LogInformation("Received Account command: Name={Name}, Deposit={Deposit}",
            context.Message.Name, context.Message.Deposit);

        return Task.CompletedTask;
    }
}
