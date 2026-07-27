namespace PropertyIntelligence.Modules.Communications.Domain;

public sealed class CommunicationRecord
{
    private CommunicationRecord()
    {
    }

    private CommunicationRecord(
        Guid id,
        Guid organizationId,
        Guid claimId,
        string communicationType,
        CommunicationDirection direction,
        CommunicationChannel channel,
        string subject,
        string recipient,
        Guid createdBy,
        DateTimeOffset createdAt)
    {
        Id = id;
        OrganizationId = RequireId(organizationId, "organization");
        ClaimId = RequireId(claimId, "claim");
        CommunicationType = Normalize(communicationType, "communication type", 100);
        Direction = RequireEnum(direction, nameof(direction));
        Channel = RequireEnum(channel, nameof(channel));
        Subject = Normalize(subject, "subject", 500);
        Recipient = Normalize(recipient, "recipient", 320);
        Status = CommunicationStatus.Draft;
        CreatedBy = RequireId(createdBy, "actor");
        CreatedAt = createdAt;
        Version = 1;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid ClaimId { get; private set; }
    public string CommunicationType { get; private set; } = string.Empty;
    public CommunicationDirection Direction { get; private set; }
    public CommunicationChannel Channel { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string Recipient { get; private set; } = string.Empty;
    public CommunicationStatus Status { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? ModifiedBy { get; private set; }
    public DateTimeOffset? ModifiedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }
    public long Version { get; private set; }

    public static CommunicationRecord Create(
        Guid organizationId,
        Guid claimId,
        string communicationType,
        CommunicationDirection direction,
        CommunicationChannel channel,
        string subject,
        string recipient,
        Guid createdBy,
        DateTimeOffset createdAt) =>
        new(
            Guid.NewGuid(),
            organizationId,
            claimId,
            communicationType,
            direction,
            channel,
            subject,
            recipient,
            createdBy,
            createdAt);

    public void ChangeStatus(
        CommunicationStatus status,
        Guid modifiedBy,
        DateTimeOffset modifiedAt)
    {
        RequireEnum(status, nameof(status));
        if (IsDeleted)
        {
            throw new InvalidOperationException("A deleted communication cannot be changed.");
        }

        if (Status is CommunicationStatus.Delivered or CommunicationStatus.Cancelled)
        {
            throw new InvalidOperationException(
                $"A {Status} communication cannot change status.");
        }

        if (!IsAllowedTransition(Status, status))
        {
            throw new InvalidOperationException(
                $"Communication status cannot transition from {Status} to {status}.");
        }

        Status = status;
        if (status is CommunicationStatus.Sent or CommunicationStatus.Delivered)
        {
            SentAt ??= modifiedAt;
        }

        ModifiedBy = RequireId(modifiedBy, "actor");
        ModifiedAt = modifiedAt;
        Version++;
    }

    private static bool IsAllowedTransition(
        CommunicationStatus current,
        CommunicationStatus target) =>
        (current, target) switch
        {
            (CommunicationStatus.Draft, CommunicationStatus.Queued) => true,
            (CommunicationStatus.Draft, CommunicationStatus.Sent) => true,
            (CommunicationStatus.Draft, CommunicationStatus.Cancelled) => true,
            (CommunicationStatus.Queued, CommunicationStatus.Sent) => true,
            (CommunicationStatus.Queued, CommunicationStatus.Failed) => true,
            (CommunicationStatus.Queued, CommunicationStatus.Cancelled) => true,
            (CommunicationStatus.Sent, CommunicationStatus.Delivered) => true,
            (CommunicationStatus.Sent, CommunicationStatus.Failed) => true,
            (CommunicationStatus.Failed, CommunicationStatus.Queued) => true,
            _ => false,
        };

    private static Guid RequireId(Guid value, string name) =>
        value != Guid.Empty
            ? value
            : throw new ArgumentException($"A {name} ID is required.", name);

    private static T RequireEnum<T>(T value, string name)
        where T : struct, Enum =>
        Enum.IsDefined(value)
            ? value
            : throw new ArgumentOutOfRangeException(name);

    private static string Normalize(string value, string name, int maximumLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            throw new ArgumentException($"A {name} is required.", name);
        }

        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException(
                $"The {name} cannot exceed {maximumLength} characters.",
                name);
        }

        return normalized;
    }
}
