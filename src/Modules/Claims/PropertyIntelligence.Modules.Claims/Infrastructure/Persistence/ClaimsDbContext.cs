using Microsoft.EntityFrameworkCore;
using PropertyIntelligence.Modules.Claims.Domain;

namespace PropertyIntelligence.Modules.Claims.Infrastructure.Persistence;

public sealed class ClaimsDbContext(DbContextOptions<ClaimsDbContext> options) : DbContext(options)
{
    public DbSet<Claim> Claims => Set<Claim>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(ClaimsSchema.Name);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClaimsDbContext).Assembly);
    }
}

internal static class ClaimsSchema
{
    public const string Name = "claims";
}
