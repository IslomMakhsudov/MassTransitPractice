using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;

namespace Producer.Data;

public class OutboxDbContext : SagaDbContext
{
    public OutboxDbContext(DbContextOptions<OutboxDbContext> options) : base(options) { }

    // SagaDbContext automatically creates the outbox tables:
    // OutboxMessage, OutboxState, InboxState
    protected override IEnumerable<ISagaClassMap> Configurations => [];
}
