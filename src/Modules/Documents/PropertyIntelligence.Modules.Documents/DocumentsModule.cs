using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using PropertyIntelligence.BuildingBlocks.Modules;
using PropertyIntelligence.Modules.Documents.Api;
using PropertyIntelligence.Modules.Documents.Contracts;
using PropertyIntelligence.Modules.Documents.Infrastructure;
using PropertyIntelligence.Modules.Documents.Infrastructure.Persistence;

namespace PropertyIntelligence.Modules.Documents;

public sealed class DocumentsModule : IModule
{
    public string Name => "Documents";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Documents")
            ?? configuration.GetConnectionString("Workflow")
            ?? throw new InvalidOperationException(
                "Connection string 'Documents' or shared connection string 'Workflow' is required.");

        services.AddDbContext<DocumentsDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", DocumentsSchema.Name))
                .UseSnakeCaseNamingConvention());
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IDocumentEvidenceReader, DocumentEvidenceReader>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/documents").WithTags(Name);

        group.MapGet("/status", () => Results.Ok(new { module = "documents", status = "ready" }));
        endpoints.MapDocumentEndpoints();
    }
}
