using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using MediatR;
using PropertyIntelligence.BuildingBlocks.Modules;
using PropertyIntelligence.Modules.Workflow.Application.Behaviors;
using PropertyIntelligence.Modules.Workflow.Application.Auditing;
using PropertyIntelligence.Modules.Workflow.Application.Definitions;
using PropertyIntelligence.Modules.Workflow.Application.Gates;
using PropertyIntelligence.Modules.Workflow.Application.Operations;
using PropertyIntelligence.Modules.Workflow.Api;
using PropertyIntelligence.Modules.Workflow.Infrastructure.Persistence;
using PropertyIntelligence.Modules.Workflow.Infrastructure.Definitions;
using PropertyIntelligence.Modules.Workflow.Infrastructure.Gates;
using PropertyIntelligence.Modules.Workflow.Infrastructure.Operations;

namespace PropertyIntelligence.Modules.Workflow;

public sealed class WorkflowModule : IModule
{
    public string Name => "Workflow";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Workflow")
            ?? throw new InvalidOperationException(
                "Connection string 'Workflow' is required to register the Workflow module.");

        services.AddDbContext<WorkflowDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", WorkflowSchema.Name))
                .UseSnakeCaseNamingConvention());

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IWorkflowDefinitionCatalog, PublishedPlaybookWorkflowDefinitionCatalog>();
        services.Configure<WorkflowOperationsOptions>(
            configuration.GetSection(WorkflowOperationsOptions.SectionName));
        services.AddScoped<INextActionCalculator, NextActionCalculator>();
        services.AddScoped<IWorkflowChangeRecorder, WorkflowChangeRecorder>();
        services.AddScoped<IWorkflowOperationalControlService, WorkflowOperationalControlService>();
        services.AddHostedService<WorkflowOperationsWorker>();
        services.AddScoped<ICompletionGateEvaluator, CompletionGateEvaluator>();
        services.AddScoped<ICompletionGateHandler, ClaimFieldPresentGateHandler>();
        services.AddScoped<ICompletionGateHandler, DocumentExistsGateHandler>();
        services.AddScoped<ICompletionGateHandler, CommunicationRecordedGateHandler>();
        services.AddScoped<ICompletionGateHandler, ApprovalGrantedGateHandler>();
        services.AddScoped<ICompletionGateHandler, TaskCompletedGateHandler>();
        services.AddScoped<ICompletionGateHandler, RuleSatisfiedGateHandler>();
        services.TryAddScoped<IClaimGateEvidenceReader, ClaimsModuleGateEvidenceReader>();
        services.TryAddScoped<IDocumentGateEvidenceReader, DocumentsModuleGateEvidenceReader>();
        services.TryAddScoped<ICommunicationGateEvidenceReader, CommunicationsModuleGateEvidenceReader>();
        services.TryAddScoped<IApprovalGateEvidenceReader, UnavailableApprovalGateEvidenceReader>();
        services.TryAddScoped<IRuleGateEvidenceReader, UnavailableRuleGateEvidenceReader>();
        services.AddValidatorsFromAssemblyContaining<WorkflowModule>(includeInternalTypes: true);
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssemblyContaining<WorkflowModule>();
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
            configuration.AddOpenBehavior(typeof(WorkflowUnitOfWorkBehavior<,>));
            configuration.AddOpenBehavior(typeof(WorkflowAuditOutboxBehavior<,>));
            configuration.AddOpenBehavior(typeof(WorkflowOperationalControlBehavior<,>));
        });
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapWorkflowEndpoints();
    }
}
