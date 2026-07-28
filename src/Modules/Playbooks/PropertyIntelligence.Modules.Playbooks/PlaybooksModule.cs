using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PropertyIntelligence.BuildingBlocks.Modules;
using PropertyIntelligence.Modules.Playbooks.Api;
using PropertyIntelligence.Modules.Playbooks.Application.Assignment;
using PropertyIntelligence.Modules.Playbooks.Contracts;
using PropertyIntelligence.Modules.Playbooks.Infrastructure;
using PropertyIntelligence.Modules.Playbooks.Infrastructure.Persistence;

namespace PropertyIntelligence.Modules.Playbooks;

public sealed class PlaybooksModule : IModule
{
    public string Name => "Playbooks";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Playbooks")
            ?? configuration.GetConnectionString("Workflow")
            ?? throw new InvalidOperationException(
                "Connection string 'Playbooks' or shared connection string 'Workflow' is required.");
        services.AddDbContext<PlaybooksDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", PlaybooksSchema.Name))
                .UseSnakeCaseNamingConvention());
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IPublishedPlaybookReader, PublishedPlaybookReader>();
        services.AddScoped<PlaybookAssignmentRecommendationService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
                "/api/playbooks/status",
                () => Results.Ok(new { module = "playbooks", status = "ready" }))
            .WithTags(Name);
        endpoints.MapPlaybookEndpoints();
    }
}
