using Microsoft.EntityFrameworkCore;
using PropertyIntelligence.Modules.Properties.Domain;

namespace PropertyIntelligence.Modules.Properties.Infrastructure.Persistence;

public sealed class PropertiesDbContext(DbContextOptions<PropertiesDbContext> options)
    : DbContext(options)
{
    public DbSet<Property> Properties => Set<Property>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");
        modelBuilder.HasDefaultSchema(PropertiesSchema.Name);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PropertiesDbContext).Assembly);
    }
}

internal static class PropertiesSchema
{
    public const string Name = "properties";
}
