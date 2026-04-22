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
    // ── Consumers ───────────────────────────────────────────────────────────
    x.AddConsumer<AccountCommandConsumer>();
    x.AddConsumer<ClientEventConsumer>();
    x.AddConsumer<TransferDataRequestConsumer>();

    // ── SAGA ────────────────────────────────────────────────────────────────
    // Регистрируем state machine + in-memory репозиторий (для продакшена — EF Core)
    x.AddSagaStateMachine<TransferStateMachine, TransferState>()
     .InMemoryRepository();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(new Uri(rabbitHost), h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPass);
        });

        // ════════════════════════════════════════════════════════════════════
        // ENDPOINT 1: AccountCommand → демонстрирует RETRY POLICY
        // ════════════════════════════════════════════════════════════════════
        cfg.ReceiveEndpoint("send-command", e =>
        {
            // RETRY: при ошибке повторить 3 раза с интервалом 2 секунды
            e.UseMessageRetry(r =>
            {
                r.Interval(retryCount: 3, interval: TimeSpan.FromSeconds(2));

                // Только для наглядности: игнорируем retry если ошибка "постоянная"
                r.Ignore<ArgumentException>();
            });

            e.ConfigureConsumer<AccountCommandConsumer>(context);
        });

        // ════════════════════════════════════════════════════════════════════
        // ENDPOINT 2: ClientEvent → демонстрирует CIRCUIT BREAKER
        // ════════════════════════════════════════════════════════════════════
        cfg.ReceiveEndpoint("publish-client-event", e =>
        {
            // CIRCUIT BREAKER:
            //   trackingPeriod  — период наблюдения (20 сек)
            //   tripThreshold   — процент ошибок для "открытия" (50%)
            //   activeThreshold — минимум попыток до оценки (5)
            //   resetInterval   — через сколько снова попробовать (30 сек)
            e.UseCircuitBreaker(cb =>
            {
                cb.TrackingPeriod = TimeSpan.FromSeconds(20);
                cb.TripThreshold = 50;
                cb.ActiveThreshold = 5;
                cb.ResetInterval = TimeSpan.FromSeconds(30);
            });

            e.ConfigureConsumer<ClientEventConsumer>(context);
        });

        // ════════════════════════════════════════════════════════════════════
        // ENDPOINT 3: TransferData Request/Response → демонстрирует OUTBOX
        // ════════════════════════════════════════════════════════════════════
        cfg.ReceiveEndpoint("transfer-data-request", e =>
        {
            // OUTBOX: сообщения публикуются только после успешного коммита обработки.
            // UseInMemoryOutbox гарантирует, что response не уйдёт если consumer упал.
            e.UseInMemoryOutbox(context);

            e.ConfigureConsumer<TransferDataRequestConsumer>(context);
        });

        // ════════════════════════════════════════════════════════════════════
        // ENDPOINT 4: Saga — демонстрирует SAGA PATTERN
        // ════════════════════════════════════════════════════════════════════
        cfg.ReceiveEndpoint("transfer-saga", e =>
        {
            e.ConfigureSaga<TransferState>(context);
        });

        // Остальные endpoints (request/response клиент для TransferData) — автоконфиг
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

