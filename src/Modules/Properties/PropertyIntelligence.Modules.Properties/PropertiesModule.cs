using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using PropertyIntelligence.BuildingBlocks.Modules;
using PropertyIntelligence.Modules.Properties.Api;
using PropertyIntelligence.Modules.Properties.Contracts;
using PropertyIntelligence.Modules.Properties.Infrastructure;
using PropertyIntelligence.Modules.Properties.Infrastructure.Persistence;

namespace PropertyIntelligence.Modules.Properties;

public sealed class PropertiesModule : IModule
{
    public string Name => "Properties";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Properties")
            ?? configuration.GetConnectionString("Workflow")
            ?? throw new InvalidOperationException(
                "Connection string 'Properties' or shared connection string 'Workflow' is required.");

        services.AddDbContext<PropertiesDbContext>(options =>
            options.UseNpgsql(
                    connectionString,
                    npgsql => npgsql
                        .UseNetTopologySuite()
                        .MigrationsHistoryTable("__ef_migrations_history", PropertiesSchema.Name))
                .UseSnakeCaseNamingConvention());
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IPropertyDirectory, PropertyDirectory>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/properties").WithTags(Name);

        group.MapGet("/status", () => Results.Ok(new { module = "properties", status = "ready" }));
        endpoints.MapPropertyEndpoints();
    }
}
