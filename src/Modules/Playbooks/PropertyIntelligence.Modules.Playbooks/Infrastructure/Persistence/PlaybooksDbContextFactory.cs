using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PropertyIntelligence.Modules.Playbooks.Infrastructure.Persistence;

public sealed class PlaybooksDbContextFactory : IDesignTimeDbContextFactory<PlaybooksDbContext>
{
    public PlaybooksDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Playbooks")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__Workflow")
            ?? "Host=localhost;Port=5432;Database=property_intelligence;Username=postgres";
        var options = new DbContextOptionsBuilder<PlaybooksDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", PlaybooksSchema.Name))
            .UseSnakeCaseNamingConvention()
            .Options;
        return new PlaybooksDbContext(options);
    }
}
