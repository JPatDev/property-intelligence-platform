using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PropertyIntelligence.BuildingBlocks.Modules;

namespace PropertyIntelligence.Modules.Properties;

public sealed class PropertiesModule : IModule
{
    public string Name => "Properties";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/properties").WithTags(Name);

        group.MapGet("/status", () => Results.Ok(new { module = "properties", status = "ready" }));
    }
}
