using PropertyIntelligence.Modules.Claims.Contracts;
using PropertyIntelligence.Modules.Communications.Contracts;
using PropertyIntelligence.Modules.Documents.Contracts;
using PropertyIntelligence.Modules.Workflow.Application.Gates;
using PropertyIntelligence.Modules.Workflow.Domain;
using PropertyIntelligence.Modules.Workflow.Infrastructure.Gates;

namespace PropertyIntelligence.Modules.Workflow.UnitTests;

public sealed class ModuleGateEvidenceReaderTests
{
    [Theory]
    [InlineData(ClaimFactEvidenceOutcome.Present, AdapterOutcome.Passed)]
    [InlineData(ClaimFactEvidenceOutcome.Missing, AdapterOutcome.Failed)]
    [InlineData(ClaimFactEvidenceOutcome.Unavailable, AdapterOutcome.Indeterminate)]
    public async Task Claim_adapter_preserves_evidence_semantics(
        ClaimFactEvidenceOutcome sourceOutcome,
        AdapterOutcome expectedOutcome)
    {
        var reader = new ClaimsModuleGateEvidenceReader(new StubClaimFactsReader(sourceOutcome));

        var result = await reader.HasFieldValueAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "PolicyNumber",
            TestContext.Current.CancellationToken);

        Assert.Equal(expectedOutcome, ToGateOutcome(result));
    }

    [Theory]
    [InlineData(DocumentEvidenceOutcome.Found, AdapterOutcome.Passed)]
    [InlineData(DocumentEvidenceOutcome.NotFound, AdapterOutcome.Failed)]
    [InlineData(DocumentEvidenceOutcome.Unavailable, AdapterOutcome.Indeterminate)]
    public async Task Document_adapter_preserves_evidence_semantics(
        DocumentEvidenceOutcome sourceOutcome,
        AdapterOutcome expectedOutcome)
    {
        var reader = new DocumentsModuleGateEvidenceReader(new StubDocumentEvidenceReader(sourceOutcome));

        var result = await reader.DocumentExistsAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "SignedPaAgreement",
            ["Verified"],
            TestContext.Current.CancellationToken);

        Assert.Equal(expectedOutcome, ToGateOutcome(result));
    }

    [Theory]
    [InlineData(CommunicationEvidenceOutcome.Found, AdapterOutcome.Passed)]
    [InlineData(CommunicationEvidenceOutcome.NotFound, AdapterOutcome.Failed)]
    [InlineData(CommunicationEvidenceOutcome.Unavailable, AdapterOutcome.Indeterminate)]
    public async Task Communication_adapter_preserves_evidence_semantics(
        CommunicationEvidenceOutcome sourceOutcome,
        AdapterOutcome expectedOutcome)
    {
        var reader = new CommunicationsModuleGateEvidenceReader(
            new StubCommunicationEvidenceReader(sourceOutcome));

        var result = await reader.CommunicationExistsAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "CarrierNotificationPacket",
            "Sent",
            TestContext.Current.CancellationToken);

        Assert.Equal(expectedOutcome, ToGateOutcome(result));
    }

    private static AdapterOutcome ToGateOutcome(GateEvidence evidence) =>
        evidence.Outcome switch
        {
            GateEvidenceOutcome.Satisfied => AdapterOutcome.Passed,
            GateEvidenceOutcome.Unsatisfied => AdapterOutcome.Failed,
            _ => AdapterOutcome.Indeterminate,
        };

    private sealed class StubClaimFactsReader(ClaimFactEvidenceOutcome outcome) : IClaimFactsReader
    {
        public Task<ClaimFactEvidence> GetFieldEvidenceAsync(
            Guid organizationId,
            Guid claimId,
            string fieldName,
            CancellationToken cancellationToken) =>
            Task.FromResult(new ClaimFactEvidence(outcome, "claim-field"));
    }

    private sealed class StubDocumentEvidenceReader(DocumentEvidenceOutcome outcome)
        : IDocumentEvidenceReader
    {
        public Task<DocumentEvidence> FindAsync(
            Guid organizationId,
            Guid claimId,
            string documentType,
            IReadOnlyCollection<string> acceptedStatuses,
            CancellationToken cancellationToken) =>
            Task.FromResult(new DocumentEvidence(outcome, "document"));
    }

    private sealed class StubCommunicationEvidenceReader(CommunicationEvidenceOutcome outcome)
        : ICommunicationEvidenceReader
    {
        public Task<CommunicationEvidence> FindAsync(
            Guid organizationId,
            Guid claimId,
            string communicationType,
            string requiredStatus,
            CancellationToken cancellationToken) =>
            Task.FromResult(new CommunicationEvidence(outcome, "communication"));
    }

    public enum AdapterOutcome
    {
        Passed,
        Failed,
        Indeterminate,
    }
}
