using PropertyIntelligence.BuildingBlocks.Modules;
using PropertyIntelligence.Modules.Billing;
using PropertyIntelligence.Modules.Claims;
using PropertyIntelligence.Modules.Documents;
using PropertyIntelligence.Modules.Identity;
using PropertyIntelligence.Modules.Organizations;
using PropertyIntelligence.Modules.Properties;
using PropertyIntelligence.Modules.Reporting;
using PropertyIntelligence.Modules.Workflow;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Property Intelligence API");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "PropertyIntelligence.Api"));

    IModule[] modules =
    [
        new IdentityModule(),
        new OrganizationsModule(),
        new PropertiesModule(),
        new ClaimsModule(),
        new DocumentsModule(),
        new WorkflowModule(),
        new ReportingModule(),
        new BillingModule(),
    ];

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("WebApp", policy =>
        {
            var allowedOrigins = builder.Configuration
                .GetSection("ClientApp:AllowedOrigins")
                .Get<string[]>() ?? [];

            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });
    builder.Services.AddModules(builder.Configuration, modules);

    var app = builder.Build();

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
        };
    });

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors("WebApp");

    app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).WithTags("Platform");
    app.MapGet("/api/modules", (IReadOnlyCollection<IModule> registeredModules) =>
        Results.Ok(registeredModules.Select(module => module.Name))).WithTags("Platform");

    app.MapModuleEndpoints(modules);

    app.Lifetime.ApplicationStarted.Register(() =>
        app.Logger.LogInformation(
            "Property Intelligence API started with {ModuleCount} modules: {Modules}",
            modules.Length,
            modules.Select(module => module.Name)));

    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "Property Intelligence API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
