using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PropertyIntelligence.BuildingBlocks.Modules;

public static class ModuleExtensions
{
    public static IServiceCollection AddModules(
        this IServiceCollection services,
        IConfiguration configuration,
        IReadOnlyCollection<IModule> modules)
    {
        foreach (var module in modules)
        {
            module.AddServices(services, configuration);
        }

        services.AddSingleton(modules);

        return services;
    }

    public static WebApplication MapModuleEndpoints(this WebApplication app, IReadOnlyCollection<IModule> modules)
    {
        foreach (var module in modules)
        {
            module.MapEndpoints(app);
        }

        return app;
    }
}
