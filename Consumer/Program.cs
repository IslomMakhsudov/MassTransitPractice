using Consumer.Consumers;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var rabbitHost = builder.Configuration["RabbitMQ:Host"]!;
var rabbitUser = builder.Configuration["RabbitMQ:Username"]!;
var rabbitPass = builder.Configuration["RabbitMQ:Password"]!;

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<AccountCommandConsumer>();
    x.AddConsumer<ClientEventConsumer>();
    x.AddConsumer<TransferDataRequestConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(new Uri(rabbitHost), h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPass);
        });

        cfg.ReceiveEndpoint("send-command", e =>
        {
            e.ConfigureConsumer<AccountCommandConsumer>(context);
        });

        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
