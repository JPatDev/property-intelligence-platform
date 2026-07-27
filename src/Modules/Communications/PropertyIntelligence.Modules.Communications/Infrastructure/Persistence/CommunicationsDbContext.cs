using Microsoft.EntityFrameworkCore;
using PropertyIntelligence.Modules.Communications.Domain;

namespace PropertyIntelligence.Modules.Communications.Infrastructure.Persistence;

public sealed class CommunicationsDbContext(
    DbContextOptions<CommunicationsDbContext> options) : DbContext(options)
{
    public DbSet<CommunicationRecord> Communications => Set<CommunicationRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(CommunicationsSchema.Name);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CommunicationsDbContext).Assembly);
    }
}

internal static class CommunicationsSchema
{
    public const string Name = "communications";
}
