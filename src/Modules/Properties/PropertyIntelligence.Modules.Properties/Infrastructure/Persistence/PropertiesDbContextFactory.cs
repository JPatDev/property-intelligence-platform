using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PropertyIntelligence.Modules.Properties.Infrastructure.Persistence;

public sealed class PropertiesDbContextFactory : IDesignTimeDbContextFactory<PropertiesDbContext>
{
    public PropertiesDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Properties")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__Workflow")
            ?? "Host=localhost;Port=5432;Database=property_intelligence;Username=postgres";

        var options = new DbContextOptionsBuilder<PropertiesDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql
                    .UseNetTopologySuite()
                    .MigrationsHistoryTable("__ef_migrations_history", PropertiesSchema.Name))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new PropertiesDbContext(options);
    }
}
