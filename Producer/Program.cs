using CommonResources;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Producer.Data;

var builder = WebApplication.CreateBuilder(args);

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

// --- Pattern 3: Outbox — SQLite database for storing outbox messages ---
builder.Services.AddDbContext<OutboxDbContext>(opts =>
    opts.UseSqlite("Data Source=producer-outbox.db"));

builder.Services.AddMassTransit(x =>
{
    // --- Pattern 3: Outbox ---
    // All IBus.Publish calls will be saved to SQLite first, then delivered to RabbitMQ
    x.AddEntityFrameworkOutbox<OutboxDbContext>(o =>
    {
        o.UseSqlite();
        o.UseBusOutbox();
    });

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

// --- Pattern 3: Outbox — create SQLite outbox tables on startup ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();
    db.Database.EnsureCreated();
}

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
