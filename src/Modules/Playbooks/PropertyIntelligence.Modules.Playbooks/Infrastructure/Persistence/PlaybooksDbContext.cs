using Microsoft.EntityFrameworkCore;
using PropertyIntelligence.Modules.Playbooks.Domain;

namespace PropertyIntelligence.Modules.Playbooks.Infrastructure.Persistence;

public sealed class PlaybooksDbContext(DbContextOptions<PlaybooksDbContext> options) : DbContext(options)
{
    public DbSet<Playbook> Playbooks => Set<Playbook>();
    public DbSet<PlaybookAssignmentEvaluationRecord> AssignmentEvaluations =>
        Set<PlaybookAssignmentEvaluationRecord>();
    public DbSet<PlaybookVersion> Versions => Set<PlaybookVersion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(PlaybooksSchema.Name);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlaybooksDbContext).Assembly);
    }
}

internal static class PlaybooksSchema
{
    public const string Name = "playbooks";
}
