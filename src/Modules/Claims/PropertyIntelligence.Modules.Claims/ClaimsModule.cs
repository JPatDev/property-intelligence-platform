using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PropertyIntelligence.BuildingBlocks.Modules;

namespace PropertyIntelligence.Modules.Claims;

public sealed class ClaimsModule : IModule
{
    public string Name => "Claims";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/claims").WithTags(Name);

        group.MapGet("/status", () => Results.Ok(new { module = "claims", status = "ready" }));
    }
}
