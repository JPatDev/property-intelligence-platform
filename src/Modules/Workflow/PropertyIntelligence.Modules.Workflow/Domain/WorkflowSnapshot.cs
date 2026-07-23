using System.Collections.ObjectModel;

namespace PropertyIntelligence.Modules.Workflow.Domain;

public sealed record WorkflowSnapshot(
    Guid SourcePlaybookId,
    Guid SourcePlaybookVersionId,
    int SchemaVersion,
    IReadOnlyList<StageSnapshot> Stages)
{
    public WorkflowSnapshot Validate()
    {
        if (SourcePlaybookId == Guid.Empty)
        {
            throw new WorkflowDomainException("workflow.playbook_id_required", "A source playbook ID is required.");
        }

        if (SourcePlaybookVersionId == Guid.Empty)
        {
            throw new WorkflowDomainException(
                "workflow.playbook_version_id_required",
                "A source playbook version ID is required.");
        }

        if (SchemaVersion < 1)
        {
            throw new WorkflowDomainException(
                "workflow.snapshot_schema_version_invalid",
                "The snapshot schema version must be at least one.");
        }

        if (Stages.Count == 0)
        {
            throw new WorkflowDomainException("workflow.stages_required", "A workflow requires at least one stage.");
        }

        EnsureUnique(Stages.Select(stage => stage.SourceDefinitionId), "workflow.duplicate_stage_definition");
        EnsureUnique(Stages.Select(stage => stage.Order), "workflow.duplicate_stage_order");

        foreach (var stage in Stages)
        {
            stage.Validate();
        }

        var taskIds = Stages.SelectMany(stage => stage.Tasks)
            .Select(task => task.SourceDefinitionId)
            .ToHashSet();

        foreach (var task in Stages.SelectMany(stage => stage.Tasks))
        {
            if (task.DependencySourceDefinitionIds.Contains(task.SourceDefinitionId))
            {
                throw new WorkflowDomainException(
                    "workflow.task_self_dependency",
                    $"Task '{task.Name}' cannot depend on itself.");
            }

            if (task.DependencySourceDefinitionIds.Any(dependencyId => !taskIds.Contains(dependencyId)))
            {
                throw new WorkflowDomainException(
                    "workflow.task_dependency_not_found",
                    $"Task '{task.Name}' contains a dependency that does not exist in the snapshot.");
            }
        }

        DetectTaskDependencyCycles(Stages.SelectMany(stage => stage.Tasks));

        return this with
        {
            Stages = new ReadOnlyCollection<StageSnapshot>(
                Stages.OrderBy(stage => stage.Order).ToArray()),
        };
    }

    private static void EnsureUnique<T>(IEnumerable<T> values, string code)
        where T : notnull
    {
        if (values.GroupBy(value => value).Any(group => group.Count() > 1))
        {
            throw new WorkflowDomainException(code, "Workflow snapshot values that must be unique were duplicated.");
        }
    }

    private static void DetectTaskDependencyCycles(IEnumerable<TaskSnapshot> tasks)
    {
        var dependencies = tasks.ToDictionary(
            task => task.SourceDefinitionId,
            task => task.DependencySourceDefinitionIds);
        var visiting = new HashSet<Guid>();
        var visited = new HashSet<Guid>();

        foreach (var taskId in dependencies.Keys)
        {
            Visit(taskId);
        }

        return;

        void Visit(Guid taskId)
        {
            if (visited.Contains(taskId))
            {
                return;
            }

            if (!visiting.Add(taskId))
            {
                throw new WorkflowDomainException(
                    "workflow.task_dependency_cycle",
                    "The workflow snapshot contains a task dependency cycle.");
            }

            foreach (var dependencyId in dependencies[taskId])
            {
                Visit(dependencyId);
            }

            visiting.Remove(taskId);
            visited.Add(taskId);
        }
    }
}

public sealed record StageSnapshot(
    Guid SourceDefinitionId,
    string Name,
    int Order,
    bool IsOptional,
    IReadOnlyList<TaskSnapshot> Tasks)
{
    internal void Validate()
    {
        if (SourceDefinitionId == Guid.Empty)
        {
            throw new WorkflowDomainException("workflow.stage_definition_id_required", "A stage definition ID is required.");
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new WorkflowDomainException("workflow.stage_name_required", "A stage name is required.");
        }

        if (Order < 0)
        {
            throw new WorkflowDomainException("workflow.stage_order_invalid", "A stage order cannot be negative.");
        }

        if (Tasks.Count == 0)
        {
            throw new WorkflowDomainException("workflow.stage_tasks_required", $"Stage '{Name}' requires at least one task.");
        }

        if (Tasks.Select(task => task.SourceDefinitionId).Distinct().Count() != Tasks.Count)
        {
            throw new WorkflowDomainException(
                "workflow.duplicate_task_definition",
                $"Stage '{Name}' contains duplicate task definition IDs.");
        }

        if (Tasks.Select(task => task.Order).Distinct().Count() != Tasks.Count)
        {
            throw new WorkflowDomainException(
                "workflow.duplicate_task_order",
                $"Stage '{Name}' contains duplicate task order values.");
        }

        foreach (var task in Tasks)
        {
            task.Validate();
        }
    }
}

public sealed record TaskSnapshot(
    Guid SourceDefinitionId,
    string Name,
    int Order,
    int Priority,
    bool IsRequired,
    IReadOnlySet<Guid> DependencySourceDefinitionIds,
    IReadOnlyList<CompletionGateDefinition> CompletionGates)
{
    internal void Validate()
    {
        if (SourceDefinitionId == Guid.Empty)
        {
            throw new WorkflowDomainException("workflow.task_definition_id_required", "A task definition ID is required.");
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new WorkflowDomainException("workflow.task_name_required", "A task name is required.");
        }

        if (Order < 0)
        {
            throw new WorkflowDomainException("workflow.task_order_invalid", "A task order cannot be negative.");
        }

        if (CompletionGates.Select(gate => gate.Id).Distinct().Count() != CompletionGates.Count)
        {
            throw new WorkflowDomainException(
                "workflow.duplicate_gate",
                $"Task '{Name}' contains duplicate completion gate IDs.");
        }

        foreach (var gate in CompletionGates)
        {
            gate.Validate();
        }
    }
}
