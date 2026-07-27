using PropertyIntelligence.Modules.Workflow.Application.Definitions;
using PropertyIntelligence.Modules.Workflow.Application.Gates;
using PropertyIntelligence.Modules.Workflow.Domain;

namespace PropertyIntelligence.Modules.Workflow.Infrastructure.Definitions;

internal sealed class BuiltInWorkflowDefinitionCatalog : IWorkflowDefinitionCatalog
{
    private const string PropertyClaimIntakeKey = "property-claim-intake";

    private static readonly IReadOnlyList<WorkflowDefinition> Definitions =
    [
        CreatePropertyClaimIntakeV1(),
    ];

    public WorkflowDefinition? Find(string key, int version) =>
        Definitions.SingleOrDefault(definition =>
            string.Equals(definition.Key, key.Trim(), StringComparison.OrdinalIgnoreCase) &&
            definition.Version == version);

    public IReadOnlyList<WorkflowDefinitionSummary> List() =>
        Definitions
            .Select(definition => new WorkflowDefinitionSummary(
                definition.Key,
                definition.Version,
                definition.Name,
                definition.Description,
                definition.Type))
            .OrderBy(definition => definition.Key, StringComparer.Ordinal)
            .ThenByDescending(definition => definition.Version)
            .ToArray();

    private static WorkflowDefinition CreatePropertyClaimIntakeV1()
    {
        var captureClaimTaskId = Guid.Parse("4dcf19bb-67a0-4fe0-9538-3f6a7ea4f17d");
        var verifyAgreementTaskId = Guid.Parse("d5df8f8c-c904-4eb8-a4e5-794770eed6f8");
        var notifyCarrierTaskId = Guid.Parse("65c6f2e1-a5c6-4fc6-82e9-050459a56dd8");

        return new WorkflowDefinition(
            PropertyClaimIntakeKey,
            1,
            "Property Claim Intake",
            "Captures core claim facts, verifies the signed agreement, and sends carrier notice.",
            WorkflowType.Primary,
            new WorkflowSnapshot(
                Guid.Parse("7ec947b4-9fab-41ed-b28c-eaf68b0ef100"),
                Guid.Parse("1d778299-d750-4a0d-bd2a-08bd7fba5701"),
                1,
                [
                    new StageSnapshot(
                        Guid.Parse("02e9872f-42ea-4d4f-9c56-f3a032f893f1"),
                        "Claim Intake",
                        1,
                        false,
                        [
                            new TaskSnapshot(
                                captureClaimTaskId,
                                "Capture claim details",
                                1,
                                100,
                                true,
                                new HashSet<Guid>(),
                                [
                                    RequiredGate(
                                        "47615de1-5058-4868-b94b-38a3ad54c4da",
                                        GateTypes.ClaimFieldPresent,
                                        new Dictionary<string, string> { ["fieldName"] = "PolicyNumber" },
                                        "POLICY_NUMBER_REQUIRED",
                                        "A policy number is required before claim intake can be completed."),
                                    RequiredGate(
                                        "29333684-a5cb-447a-973c-570cdaaf1e5d",
                                        GateTypes.ClaimFieldPresent,
                                        new Dictionary<string, string> { ["fieldName"] = "DateOfLoss" },
                                        "DATE_OF_LOSS_REQUIRED",
                                        "A date of loss is required before claim intake can be completed."),
                                ]),
                            new TaskSnapshot(
                                verifyAgreementTaskId,
                                "Verify signed public adjusting agreement",
                                2,
                                90,
                                true,
                                new HashSet<Guid> { captureClaimTaskId },
                                [
                                    RequiredGate(
                                        "a2e09a12-db9f-416a-bb8c-7ec7d306f9cb",
                                        GateTypes.DocumentExists,
                                        new Dictionary<string, string>
                                        {
                                            ["documentType"] = "SignedPaAgreement",
                                            ["acceptedStatuses"] = "Verified",
                                        },
                                        "SIGNED_PA_AGREEMENT_REQUIRED",
                                        "A verified signed public adjusting agreement is required."),
                                ]),
                        ]),
                    new StageSnapshot(
                        Guid.Parse("68eb6cb6-677b-4616-a538-ae591f53ab65"),
                        "Carrier Notification",
                        2,
                        false,
                        [
                            new TaskSnapshot(
                                notifyCarrierTaskId,
                                "Send carrier notification packet",
                                1,
                                80,
                                true,
                                new HashSet<Guid> { verifyAgreementTaskId },
                                [
                                    RequiredGate(
                                        "256f373f-b350-41bb-b622-7f356648489f",
                                        GateTypes.CommunicationRecorded,
                                        new Dictionary<string, string>
                                        {
                                            ["communicationType"] = "CarrierNotificationPacket",
                                            ["requiredStatus"] = "Sent",
                                        },
                                        "CARRIER_NOTIFICATION_REQUIRED",
                                        "The carrier notification packet must be recorded as sent."),
                                ]),
                        ]),
                ]));
    }

    private static CompletionGateDefinition RequiredGate(
        string id,
        string gateType,
        IReadOnlyDictionary<string, string> parameters,
        string failureCode,
        string failureMessage) =>
        new(
            Guid.Parse(id),
            gateType,
            CompletionGateScope.Task,
            CompletionGateSeverity.Required,
            parameters,
            failureCode,
            failureMessage,
            1);
}
