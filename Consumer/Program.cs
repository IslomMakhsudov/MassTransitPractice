using Consumer.Consumers;
using Consumer.Sagas;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var rabbitHost = builder.Configuration["RabbitMQ:Host"]!;
var rabbitUser = builder.Configuration["RabbitMQ:Username"]!;
var rabbitPass = builder.Configuration["RabbitMQ:Password"]!;

builder.Services.AddMassTransit(x =>
{
  
    x.AddConsumers(typeof(Program).Assembly);
    // --- Pattern 1: Retry ---
    x.AddConsumer<RetryCommandConsumer>();

    // --- Pattern 3: Outbox (consumer side receives the delivered message) ---
    x.AddConsumer<OutboxMessageConsumer>();

    // --- Pattern 2: Circuit Breaker ---
    x.AddConsumer<CircuitBreakerCommandConsumer>();

    // --- Pattern 4: Saga ---
    x.AddSagaStateMachine<OrderStateMachine, OrderState>()
     .InMemoryRepository();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(new Uri(rabbitHost), h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPass);
        });

        // --- Existing endpoint ---
        cfg.ReceiveEndpoint("send-command", e =>
        {
            e.ConfigureConsumer<AccountCommandConsumer>(context);
        });

        // --- Pattern 1: Retry ---
        // UseMessageRetry MUST be before ConfigureConsumer (middleware runs in order)
        cfg.ReceiveEndpoint("retry-demo", e =>
        {
            e.UseMessageRetry(r => r.Intervals(
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(4),
                TimeSpan.FromSeconds(6)));

            e.ConfigureConsumer<RetryCommandConsumer>(context);
        });

        // --- Pattern 2: Circuit Breaker ---
        // Trip threshold: if >50% of last N calls fail → circuit opens
        // Reset interval: after 15s the circuit tries to close again
        cfg.ReceiveEndpoint("circuit-breaker-demo", e =>
        {
            e.UseCircuitBreaker(cb =>
            {
                cb.TrackingPeriod = TimeSpan.FromSeconds(30);
                cb.TripThreshold = 50;   // open circuit when 50% of calls fail
                cb.ActiveThreshold = 3;  // need at least 3 calls before evaluating
                cb.ResetInterval = TimeSpan.FromSeconds(15);
            });

            e.ConfigureConsumer<CircuitBreakerCommandConsumer>(context);
        });

        // --- Pattern 4: Saga ---
        cfg.ReceiveEndpoint("order-saga", e =>
        {
            e.ConfigureSaga<OrderState>(context);
        });

        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
