using CommonResources;

using MassTransit;

namespace Consumer.Consumers;

public class TransferDataRequestConsumer : IConsumer<TransferData>
{
    private readonly ILogger<TransferDataRequestConsumer> _logger;

    public TransferDataRequestConsumer(ILogger<TransferDataRequestConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TransferData> context)
    {
        _logger.LogInformation("Received TransferData request: Type={Type}, Amount={Amount}",
            context.Message.Type, context.Message.Amount);

        var balance = new CurrentBalance
        {
            Balance = 1000 - context.Message.Amount
        };

        await context.RespondAsync(balance);
    }
}
