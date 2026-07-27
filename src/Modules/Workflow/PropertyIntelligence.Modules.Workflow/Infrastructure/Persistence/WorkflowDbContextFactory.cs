using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PropertyIntelligence.Modules.Workflow.Infrastructure.Persistence;

public sealed class WorkflowDbContextFactory : IDesignTimeDbContextFactory<WorkflowDbContext>
{
    public WorkflowDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Workflow")
            ?? "Host=localhost;Port=5432;Database=property_intelligence;Username=postgres";

        var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", WorkflowSchema.Name))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new WorkflowDbContext(options);
    }
}
