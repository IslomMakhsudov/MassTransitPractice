using MassTransit;

using PaymentService.Simulation;

using SharedContracts;

namespace PaymentService.Consumers;

public class ProcessPaymentConsumer(IFailureSimulator failureSimulator) : IConsumer<ProcessPaymentRequested>
{
    public async Task Consume(ConsumeContext<ProcessPaymentRequested> context)
    {
        if (failureSimulator.ShouldFail())
        {
            await context.Publish(new PaymentFailed
            {
                OrderId = context.Message.OrderId,
                Reason = "Payment declined by processor"
            });
        }
        else
        {
            await context.Publish(new PaymentProcessed
            {
                OrderId = context.Message.OrderId
            });
        }
    }
}
