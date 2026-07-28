using PropertyIntelligence.Modules.Playbooks.Contracts;

namespace PropertyIntelligence.Modules.Playbooks.Seed;

public static class InitialPlaybooks
{
    public const string PropertyClaimIntakeKey = "property-claim-intake";

    public static IReadOnlyList<PlaybookStageDefinition> PropertyClaimIntakeStages()
    {
        var captureClaim = Guid.Parse("4dcf19bb-67a0-4fe0-9538-3f6a7ea4f17d");
        var verifyAgreement = Guid.Parse("d5df8f8c-c904-4eb8-a4e5-794770eed6f8");
        return
        [
            new PlaybookStageDefinition(
                Guid.Parse("02e9872f-42ea-4d4f-9c56-f3a032f893f1"),
                "Claim Intake",
                1,
                false,
                [
                    new PlaybookTaskDefinition(
                        captureClaim,
                        "Capture claim details",
                        1,
                        100,
                        true,
                        [],
                        [
                            Gate(
                                "47615de1-5058-4868-b94b-38a3ad54c4da",
                                "ClaimFieldPresent",
                                new Dictionary<string, string> { ["fieldName"] = "PolicyNumber" },
                                "POLICY_NUMBER_REQUIRED",
                                "A policy number is required before claim intake can be completed."),
                            Gate(
                                "29333684-a5cb-447a-973c-570cdaaf1e5d",
                                "ClaimFieldPresent",
                                new Dictionary<string, string> { ["fieldName"] = "DateOfLoss" },
                                "DATE_OF_LOSS_REQUIRED",
                                "A date of loss is required before claim intake can be completed."),
                        ]),
                    new PlaybookTaskDefinition(
                        verifyAgreement,
                        "Verify signed public adjusting agreement",
                        2,
                        90,
                        true,
                        [captureClaim],
                        [
                            Gate(
                                "a2e09a12-db9f-416a-bb8c-7ec7d306f9cb",
                                "DocumentExists",
                                new Dictionary<string, string>
                                {
                                    ["documentType"] = "SignedPaAgreement",
                                    ["acceptedStatuses"] = "Verified",
                                },
                                "SIGNED_PA_AGREEMENT_REQUIRED",
                                "A verified signed public adjusting agreement is required."),
                        ]),
                ]),
            new PlaybookStageDefinition(
                Guid.Parse("68eb6cb6-677b-4616-a538-ae591f53ab65"),
                "Carrier Notification",
                2,
                false,
                [
                    new PlaybookTaskDefinition(
                        Guid.Parse("65c6f2e1-a5c6-4fc6-82e9-050459a56dd8"),
                        "Send carrier notification packet",
                        1,
                        80,
                        true,
                        [verifyAgreement],
                        [
                            Gate(
                                "256f373f-b350-41bb-b622-7f356648489f",
                                "CommunicationRecorded",
                                new Dictionary<string, string>
                                {
                                    ["communicationType"] = "CarrierNotificationPacket",
                                    ["requiredStatus"] = "Sent",
                                },
                                "CARRIER_NOTIFICATION_REQUIRED",
                                "The carrier notification packet must be recorded as sent."),
                        ]),
                ]),
        ];
    }

    private static PlaybookGateDefinition Gate(
        string id,
        string type,
        IReadOnlyDictionary<string, string> parameters,
        string failureCode,
        string failureMessage) =>
        new(
            Guid.Parse(id),
            type,
            "Task",
            "Required",
            parameters,
            failureCode,
            failureMessage,
            1);
}
