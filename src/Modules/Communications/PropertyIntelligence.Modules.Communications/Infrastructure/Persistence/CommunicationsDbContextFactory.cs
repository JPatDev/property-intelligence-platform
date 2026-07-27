using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PropertyIntelligence.Modules.Communications.Infrastructure.Persistence;

public sealed class CommunicationsDbContextFactory
    : IDesignTimeDbContextFactory<CommunicationsDbContext>
{
    public CommunicationsDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Communications")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__Workflow")
            ?? "Host=localhost;Port=5432;Database=property_intelligence;Username=postgres";

        var options = new DbContextOptionsBuilder<CommunicationsDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    CommunicationsSchema.Name))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new CommunicationsDbContext(options);
    }
}
