namespace PropertyIntelligence.Modules.Documents.Domain;

public sealed class DocumentRecord
{
    private DocumentRecord()
    {
    }

    private DocumentRecord(
        Guid id,
        Guid organizationId,
        Guid claimId,
        string documentType,
        string fileName,
        string storagePath,
        Guid createdBy,
        DateTimeOffset createdAt)
    {
        Id = id;
        OrganizationId = RequireId(organizationId, "organization");
        ClaimId = RequireId(claimId, "claim");
        DocumentType = Normalize(documentType, "document type", 100);
        FileName = Normalize(fileName, "file name", 255);
        StoragePath = Normalize(storagePath, "storage path", 1000);
        Status = DocumentStatus.Uploaded;
        CreatedBy = RequireId(createdBy, "actor");
        CreatedAt = createdAt;
        Version = 1;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid ClaimId { get; private set; }
    public string DocumentType { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string StoragePath { get; private set; } = string.Empty;
    public DocumentStatus Status { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? ModifiedBy { get; private set; }
    public DateTimeOffset? ModifiedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }
    public long Version { get; private set; }

    public static DocumentRecord Register(
        Guid organizationId,
        Guid claimId,
        string documentType,
        string fileName,
        string storagePath,
        Guid createdBy,
        DateTimeOffset createdAt) =>
        new(
            Guid.NewGuid(),
            organizationId,
            claimId,
            documentType,
            fileName,
            storagePath,
            createdBy,
            createdAt);

    public void ChangeStatus(
        DocumentStatus status,
        Guid modifiedBy,
        DateTimeOffset modifiedAt)
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException("A deleted document cannot be changed.");
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        if (Status == DocumentStatus.Archived)
        {
            throw new InvalidOperationException("An archived document cannot change status.");
        }

        Status = status;
        ModifiedBy = RequireId(modifiedBy, "actor");
        ModifiedAt = modifiedAt;
        Version++;
    }

    private static Guid RequireId(Guid value, string name) =>
        value != Guid.Empty
            ? value
            : throw new ArgumentException($"A {name} ID is required.", name);

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
