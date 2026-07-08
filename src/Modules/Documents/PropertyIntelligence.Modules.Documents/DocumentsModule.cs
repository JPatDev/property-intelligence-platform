using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PropertyIntelligence.BuildingBlocks.Modules;

namespace PropertyIntelligence.Modules.Documents;

public sealed class DocumentsModule : IModule
{
    public string Name => "Documents";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/documents").WithTags(Name);

        group.MapGet("/status", () => Results.Ok(new { module = "documents", status = "ready" }));
    }
}
