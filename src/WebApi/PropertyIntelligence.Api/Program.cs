using PropertyIntelligence.BuildingBlocks.Modules;
using PropertyIntelligence.Modules.Billing;
using PropertyIntelligence.Modules.Claims;
using PropertyIntelligence.Modules.Documents;
using PropertyIntelligence.Modules.Identity;
using PropertyIntelligence.Modules.Organizations;
using PropertyIntelligence.Modules.Properties;
using PropertyIntelligence.Modules.Reporting;
using PropertyIntelligence.Modules.Workflow;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddModules(builder.Configuration, modules);

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).WithTags("Platform");
app.MapGet("/api/modules", (IReadOnlyCollection<IModule> registeredModules) =>
    Results.Ok(registeredModules.Select(module => module.Name))).WithTags("Platform");

app.MapModuleEndpoints(modules);

app.Run();
