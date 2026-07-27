using Microsoft.EntityFrameworkCore;
using PropertyIntelligence.Modules.Workflow.Domain;

namespace PropertyIntelligence.Modules.Workflow.Infrastructure.Persistence;

public sealed class WorkflowDbContext(DbContextOptions<WorkflowDbContext> options) : DbContext(options)
{
    public DbSet<WorkflowInstance> Workflows => Set<WorkflowInstance>();
    public DbSet<NextActionSnapshot> NextActions => Set<NextActionSnapshot>();
    public DbSet<WorkflowEscalation> Escalations => Set<WorkflowEscalation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(WorkflowSchema.Name);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorkflowDbContext).Assembly);
    }
}

internal static class WorkflowSchema
{
    public const string Name = "workflow";
}
