using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PropertyIntelligence.Modules.Documents.Infrastructure.Persistence;

public sealed class DocumentsDbContextFactory : IDesignTimeDbContextFactory<DocumentsDbContext>
{
    public DocumentsDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Documents")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__Workflow")
            ?? "Host=localhost;Port=5432;Database=property_intelligence;Username=postgres";

        var options = new DbContextOptionsBuilder<DocumentsDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", DocumentsSchema.Name))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new DocumentsDbContext(options);
    }
}
