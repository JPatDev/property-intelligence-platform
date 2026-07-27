using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using PropertyIntelligence.BuildingBlocks.Modules;
using PropertyIntelligence.Modules.Communications.Api;
using PropertyIntelligence.Modules.Communications.Contracts;
using PropertyIntelligence.Modules.Communications.Infrastructure;
using PropertyIntelligence.Modules.Communications.Infrastructure.Persistence;

namespace PropertyIntelligence.Modules.Communications;

public sealed class CommunicationsModule : IModule
{
    public string Name => "Communications";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Communications")
            ?? configuration.GetConnectionString("Workflow")
            ?? throw new InvalidOperationException(
                "Connection string 'Communications' or shared connection string 'Workflow' is required.");

        services.AddDbContext<CommunicationsDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsHistoryTable(
                        "__ef_migrations_history",
                        CommunicationsSchema.Name))
                .UseSnakeCaseNamingConvention());
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ICommunicationEvidenceReader, CommunicationEvidenceReader>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/communications").WithTags(Name);
        group.MapGet("/status", () => Results.Ok(new { module = "communications", status = "ready" }));
        endpoints.MapCommunicationEndpoints();
    }
}
