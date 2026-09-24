namespace PropertyIntelligence.BuildingBlocks.Security;

public interface IRequestIdentity
{
    Guid UserId { get; }

    Guid OrganizationId { get; }
}

public static class PlatformClaimTypes
{
    public const string OrganizationId = "org_id";
    public const string ObjectId = "oid";
}

public static class PlatformRoles
{
    public const string Owner = "Owner";
    public const string Administrator = "Administrator";
    public const string Manager = "Manager";
    public const string PublicAdjuster = "PublicAdjuster";
    public const string Assistant = "Assistant";
    public const string Attorney = "Attorney";
    public const string ReadOnly = "ReadOnly";
}

public static class PlatformPolicies
{
    public const string TenantAccess = "platform.tenant_access";
    public const string ManageProperties = "platform.manage_properties";
    public const string ManageClaims = "platform.manage_claims";
    public const string ManageDocuments = "platform.manage_documents";
    public const string ManageCommunications = "platform.manage_communications";
    public const string ManageWorkflow = "platform.manage_workflow";
    public const string ManagePlaybooks = "platform.manage_playbooks";
    public const string ViewWorkflowAudit = "platform.view_workflow_audit";
}
