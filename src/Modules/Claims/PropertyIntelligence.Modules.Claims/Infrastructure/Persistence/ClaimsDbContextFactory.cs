using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PropertyIntelligence.Modules.Claims.Infrastructure.Persistence;

public sealed class ClaimsDbContextFactory : IDesignTimeDbContextFactory<ClaimsDbContext>
{
    public ClaimsDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Claims")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__Workflow")
            ?? "Host=localhost;Port=5432;Database=property_intelligence;Username=postgres";

        var options = new DbContextOptionsBuilder<ClaimsDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", ClaimsSchema.Name))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new ClaimsDbContext(options);
    }
}
