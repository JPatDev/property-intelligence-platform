using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PropertyIntelligence.BuildingBlocks.Modules;

namespace PropertyIntelligence.Modules.Billing;

public sealed class BillingModule : IModule
{
    public string Name => "Billing";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/billing").WithTags(Name);

        group.MapGet("/status", () => Results.Ok(new { module = "billing", status = "ready" }));
    }
}
