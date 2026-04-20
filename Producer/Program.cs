using CommonResources;

using MassTransit;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Producer API", Version = "v1" });
});

var rabbitHost = builder.Configuration["RabbitMQ:Host"]!;
var rabbitUser = builder.Configuration["RabbitMQ:Username"]!;
var rabbitPass = builder.Configuration["RabbitMQ:Password"]!;

builder.Services.AddMassTransit(x =>
{
    x.AddRequestClient<TransferData>();
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(new Uri(rabbitHost), h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPass);
        });

        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Producer API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
