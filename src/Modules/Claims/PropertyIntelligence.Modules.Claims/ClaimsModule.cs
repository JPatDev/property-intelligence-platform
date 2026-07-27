using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using PropertyIntelligence.BuildingBlocks.Modules;
using PropertyIntelligence.Modules.Claims.Api;
using PropertyIntelligence.Modules.Claims.Contracts;
using PropertyIntelligence.Modules.Claims.Infrastructure;
using PropertyIntelligence.Modules.Claims.Infrastructure.Persistence;

namespace PropertyIntelligence.Modules.Claims;

public sealed class ClaimsModule : IModule
{
    public string Name => "Claims";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Claims")
            ?? configuration.GetConnectionString("Workflow")
            ?? throw new InvalidOperationException(
                "Connection string 'Claims' or shared connection string 'Workflow' is required.");

        services.AddDbContext<ClaimsDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", ClaimsSchema.Name))
                .UseSnakeCaseNamingConvention());
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IClaimFactsReader, ClaimFactsReader>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/claims").WithTags(Name);

        group.MapGet("/status", () => Results.Ok(new { module = "claims", status = "ready" }));
        endpoints.MapClaimEndpoints();
    }
}
