namespace PropertyIntelligence.Modules.Playbooks.Domain;

public enum PlaybookStatus
{
    Draft = 1,
    Active = 2,
    Deprecated = 3,
    Archived = 4,
}

public enum PlaybookVersionStatus
{
    Draft = 1,
    Published = 2,
    Deprecated = 3,
}
