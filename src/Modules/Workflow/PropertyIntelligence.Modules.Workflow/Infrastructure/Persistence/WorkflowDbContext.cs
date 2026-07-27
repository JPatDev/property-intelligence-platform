using Microsoft.EntityFrameworkCore;
using PropertyIntelligence.Modules.Workflow.Domain;

namespace PropertyIntelligence.Modules.Workflow.Infrastructure.Persistence;

public sealed class WorkflowDbContext(DbContextOptions<WorkflowDbContext> options) : DbContext(options)
{
    public DbSet<WorkflowInstance> Workflows => Set<WorkflowInstance>();
    public DbSet<NextActionSnapshot> NextActions => Set<NextActionSnapshot>();
    public DbSet<WorkflowEscalation> Escalations => Set<WorkflowEscalation>();
    public DbSet<WorkflowAuditRecord> AuditRecords => Set<WorkflowAuditRecord>();
    public DbSet<WorkflowOutboxMessage> OutboxMessages => Set<WorkflowOutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(WorkflowSchema.Name);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorkflowDbContext).Assembly);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnforceAppendOnlyRecords();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        EnforceAppendOnlyRecords();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void EnforceAppendOnlyRecords()
    {
        if (ChangeTracker.Entries<WorkflowAuditRecord>()
            .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException("Workflow audit records are append-only.");
        }
    }
}

internal static class WorkflowSchema
{
    public const string Name = "workflow";
}
