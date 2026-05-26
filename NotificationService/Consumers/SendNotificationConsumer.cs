using MassTransit;
using SharedContracts;

namespace NotificationService.Consumers;

public class SendNotificationConsumer(ILogger<SendNotificationConsumer> logger) : IConsumer<SendNotificationRequested>
{
    public async Task Consume(ConsumeContext<SendNotificationRequested> context)
    {
        var msg = context.Message;
        logger.LogInformation(
            "Sending email to {Email} | Subject: {Subject} | Body: {Body}",
            msg.CustomerEmail, msg.Subject, msg.Body);

        await context.Publish(new NotificationSent { OrderId = msg.OrderId });
    }
}
