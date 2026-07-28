namespace PropertyIntelligence.Modules.Playbooks.Domain;

public sealed class PlaybookAssignmentEvaluationRecord
{
    private PlaybookAssignmentEvaluationRecord()
    {
    }

    internal PlaybookAssignmentEvaluationRecord(
        Guid organizationId,
        Guid claimId,
        Guid evaluatedBy,
        DateTimeOffset evaluatedAt,
        string resultsJson)
    {
        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        ClaimId = claimId;
        EvaluatedBy = evaluatedBy;
        EvaluatedAt = evaluatedAt;
        ResultsJson = resultsJson;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid ClaimId { get; private set; }
    public Guid EvaluatedBy { get; private set; }
    public DateTimeOffset EvaluatedAt { get; private set; }
    public string ResultsJson { get; private set; } = "[]";
}
