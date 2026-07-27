namespace PropertyIntelligence.Modules.Communications.Domain;

public enum CommunicationDirection
{
    Outbound = 1,
    Inbound = 2,
}

public enum CommunicationChannel
{
    Email = 1,
    Sms = 2,
    Phone = 3,
    Letter = 4,
    Portal = 5,
}

public enum CommunicationStatus
{
    Draft = 1,
    Queued = 2,
    Sent = 3,
    Delivered = 4,
    Failed = 5,
    Cancelled = 6,
}
