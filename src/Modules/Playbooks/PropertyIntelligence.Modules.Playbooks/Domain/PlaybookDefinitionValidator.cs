using PropertyIntelligence.Modules.Playbooks.Contracts;

namespace PropertyIntelligence.Modules.Playbooks.Domain;

internal static class PlaybookDefinitionValidator
{
    public static void Validate(
        string workflowType,
        int schemaVersion,
        IReadOnlyList<PlaybookStageDefinition> stages,
        IReadOnlyList<PlaybookAssignmentRuleDefinition>? assignmentRules = null)
    {
        if (workflowType is not ("Primary" or "Supplemental"))
        {
            throw new ArgumentException("Workflow type must be Primary or Supplemental.");
        }

        if (schemaVersion < 1)
        {
            throw new ArgumentException("Schema version must be at least one.");
        }

        if (stages.Count == 0)
        {
            throw new ArgumentException("A playbook requires at least one stage.");
        }

        EnsureUnique(stages.Select(stage => stage.Id), "stage IDs");
        EnsureUnique(stages.Select(stage => stage.Order), "stage orders");
        var tasks = stages.SelectMany(stage => stage.Tasks).ToArray();
        EnsureUnique(tasks.Select(task => task.Id), "task IDs");
        var taskIds = tasks.Select(task => task.Id).ToHashSet();

        foreach (var stage in stages)
        {
            if (stage.Id == Guid.Empty || string.IsNullOrWhiteSpace(stage.Name) || stage.Tasks.Count == 0)
            {
                throw new ArgumentException("Every stage requires an ID, name, and at least one task.");
            }

            EnsureUnique(stage.Tasks.Select(task => task.Order), $"task orders in stage '{stage.Name}'");
        }

        foreach (var task in tasks)
        {
            if (task.Id == Guid.Empty || string.IsNullOrWhiteSpace(task.Name))
            {
                throw new ArgumentException("Every task requires an ID and name.");
            }

            if (task.DependencyIds.Contains(task.Id) ||
                task.DependencyIds.Any(dependency => !taskIds.Contains(dependency)))
            {
                throw new ArgumentException($"Task '{task.Name}' has an invalid dependency.");
            }

            EnsureUnique(task.CompletionGates.Select(gate => gate.Id), $"gates on task '{task.Name}'");
            foreach (var gate in task.CompletionGates)
            {
                if (gate.Id == Guid.Empty ||
                    string.IsNullOrWhiteSpace(gate.GateType) ||
                    string.IsNullOrWhiteSpace(gate.FailureCode) ||
                    string.IsNullOrWhiteSpace(gate.FailureMessage) ||
                    gate.EvaluationVersion < 1)
                {
                    throw new ArgumentException($"Task '{task.Name}' has an invalid completion gate.");
                }
            }
        }

        DetectCycles(tasks);
        ValidateAssignmentRules(assignmentRules ?? []);
    }

    private static void ValidateAssignmentRules(
        IReadOnlyList<PlaybookAssignmentRuleDefinition> rules)
    {
        EnsureUnique(rules.Select(rule => rule.Id), "assignment rule IDs");
        foreach (var rule in rules)
        {
            if (rule.Id == Guid.Empty || string.IsNullOrWhiteSpace(rule.Name))
            {
                throw new ArgumentException("Every assignment rule requires an ID and name.");
            }

            ValidateGroup(rule.When);
        }
    }

    private static void ValidateGroup(PlaybookConditionGroup group)
    {
        if (group.Match is null ||
            !group.Match.Equals("all", StringComparison.OrdinalIgnoreCase) &&
            !group.Match.Equals("any", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("An assignment condition group must match 'all' or 'any'.");
        }

        if (group.Conditions.Count == 0 && (group.Groups?.Count ?? 0) == 0)
        {
            throw new ArgumentException("An assignment condition group cannot be empty.");
        }

        foreach (var condition in group.Conditions)
        {
            if (!PlaybookAssignmentFacts.Supported.Contains(condition.Fact))
            {
                throw new ArgumentException($"Assignment fact '{condition.Fact}' is not supported.");
            }

            if (!PlaybookAssignmentOperators.Supported.Contains(condition.Operator))
            {
                throw new ArgumentException(
                    $"Assignment operator '{condition.Operator}' is not supported.");
            }

            var requiresValues =
                !condition.Operator.Equals(
                    PlaybookAssignmentOperators.Exists,
                    StringComparison.OrdinalIgnoreCase) &&
                !condition.Operator.Equals(
                    PlaybookAssignmentOperators.NotExists,
                    StringComparison.OrdinalIgnoreCase);
            if (requiresValues && (condition.Values is null || condition.Values.Count == 0))
            {
                throw new ArgumentException(
                    $"Assignment condition '{condition.Fact}' requires at least one value.");
            }
        }

        foreach (var child in group.Groups ?? [])
        {
            ValidateGroup(child);
        }
    }

    private static void EnsureUnique<T>(IEnumerable<T> values, string name)
        where T : notnull
    {
        if (values.GroupBy(value => value).Any(group => group.Count() > 1))
        {
            throw new ArgumentException($"Playbook {name} must be unique.");
        }
    }

    private static void DetectCycles(IReadOnlyCollection<PlaybookTaskDefinition> tasks)
    {
        var dependencies = tasks.ToDictionary(task => task.Id, task => task.DependencyIds);
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
                throw new ArgumentException("The playbook contains a task dependency cycle.");
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
