using FluentValidation;
using System.Linq.Expressions;

namespace PropertyIntelligence.Modules.Workflow.Application.Commands;

internal abstract class ExistingWorkflowCommandValidator<T> : AbstractValidator<T>
{
    protected void ValidateWorkflowIdentity(
        Expression<Func<T, Guid>> organizationId,
        Expression<Func<T, Guid>> workflowId,
        Expression<Func<T, long>> expectedVersion)
    {
        RuleFor(organizationId).NotEmpty().WithName("OrganizationId");
        RuleFor(workflowId).NotEmpty().WithName("WorkflowId");
        RuleFor(expectedVersion).GreaterThan(0).WithName("ExpectedVersion");
    }
}

internal sealed class CreateWorkflowCommandValidator : AbstractValidator<CreateWorkflowCommand>
{
    public CreateWorkflowCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.ClaimId).NotEmpty();
        RuleFor(command => command.Type).IsInEnum();
        RuleFor(command => command.DefinitionKey).NotEmpty().MaximumLength(100);
        RuleFor(command => command.DefinitionVersion).GreaterThan(0);
    }
}

internal sealed class StartWorkflowCommandValidator : ExistingWorkflowCommandValidator<StartWorkflowCommand>
{
    public StartWorkflowCommandValidator() =>
        ValidateWorkflowIdentity(
            command => command.OrganizationId,
            command => command.WorkflowId,
            command => command.ExpectedVersion);
}

internal sealed class AssignTaskCommandValidator : ExistingWorkflowCommandValidator<AssignTaskCommand>
{
    public AssignTaskCommandValidator()
    {
        ValidateWorkflowIdentity(
            command => command.OrganizationId,
            command => command.WorkflowId,
            command => command.ExpectedVersion);
        RuleFor(command => command.TaskId).NotEmpty();
        RuleFor(command => command.AssigneeId).NotEmpty();
    }
}

internal sealed class StartTaskCommandValidator : ExistingWorkflowCommandValidator<StartTaskCommand>
{
    public StartTaskCommandValidator()
    {
        ValidateWorkflowIdentity(
            command => command.OrganizationId,
            command => command.WorkflowId,
            command => command.ExpectedVersion);
        RuleFor(command => command.TaskId).NotEmpty();
    }
}

internal sealed class CompleteTaskCommandValidator : ExistingWorkflowCommandValidator<CompleteTaskCommand>
{
    public CompleteTaskCommandValidator()
    {
        ValidateWorkflowIdentity(
            command => command.OrganizationId,
            command => command.WorkflowId,
            command => command.ExpectedVersion);
        RuleFor(command => command.TaskId).NotEmpty();
        RuleFor(command => command.ActorId).NotEmpty();
    }
}

internal sealed class AddTaskBlockerCommandValidator : ExistingWorkflowCommandValidator<AddTaskBlockerCommand>
{
    public AddTaskBlockerCommandValidator()
    {
        ValidateWorkflowIdentity(
            command => command.OrganizationId,
            command => command.WorkflowId,
            command => command.ExpectedVersion);
        RuleFor(command => command.TaskId).NotEmpty();
        RuleFor(command => command.ActorId).NotEmpty();
        RuleFor(command => command.Code).NotEmpty().MaximumLength(150);
        RuleFor(command => command.Description).NotEmpty().MaximumLength(2000);
    }
}

internal sealed class ResolveTaskBlockerCommandValidator
    : ExistingWorkflowCommandValidator<ResolveTaskBlockerCommand>
{
    public ResolveTaskBlockerCommandValidator()
    {
        ValidateWorkflowIdentity(
            command => command.OrganizationId,
            command => command.WorkflowId,
            command => command.ExpectedVersion);
        RuleFor(command => command.TaskId).NotEmpty();
        RuleFor(command => command.BlockerId).NotEmpty();
        RuleFor(command => command.ActorId).NotEmpty();
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(1000);
    }
}

internal sealed class SkipStageCommandValidator : ExistingWorkflowCommandValidator<SkipStageCommand>
{
    public SkipStageCommandValidator()
    {
        ValidateWorkflowIdentity(
            command => command.OrganizationId,
            command => command.WorkflowId,
            command => command.ExpectedVersion);
        RuleFor(command => command.StageId).NotEmpty();
        RuleFor(command => command.ActorId).NotEmpty();
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(1000);
    }
}

internal sealed class CancelWorkflowCommandValidator : ExistingWorkflowCommandValidator<CancelWorkflowCommand>
{
    public CancelWorkflowCommandValidator()
    {
        ValidateWorkflowIdentity(
            command => command.OrganizationId,
            command => command.WorkflowId,
            command => command.ExpectedVersion);
        RuleFor(command => command.ActorId).NotEmpty();
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(1000);
    }
}

internal sealed class ArchiveWorkflowCommandValidator : ExistingWorkflowCommandValidator<ArchiveWorkflowCommand>
{
    public ArchiveWorkflowCommandValidator() =>
        ValidateWorkflowIdentity(
            command => command.OrganizationId,
            command => command.WorkflowId,
            command => command.ExpectedVersion);
}
