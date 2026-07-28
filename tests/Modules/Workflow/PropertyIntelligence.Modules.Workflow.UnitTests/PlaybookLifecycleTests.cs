using PropertyIntelligence.Modules.Playbooks.Domain;
using PropertyIntelligence.Modules.Playbooks.Seed;

namespace PropertyIntelligence.Modules.Workflow.UnitTests;

public sealed class PlaybookLifecycleTests
{
    [Fact]
    public void Published_version_is_immutable()
    {
        var actorId = Guid.NewGuid();
        var playbook = CreatePlaybook(actorId);
        var version = playbook.Versions.Single();
        playbook.Publish(version.Id, actorId, DateTimeOffset.UtcNow);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            playbook.UpdateDraft(
                version.Id,
                playbook.Name,
                playbook.Description,
                "Primary",
                1,
                InitialPlaybooks.PropertyClaimIntakeStages(),
                actorId,
                DateTimeOffset.UtcNow));

        Assert.Equal("Published playbook versions are immutable.", exception.Message);
    }

    [Fact]
    public void Publishing_new_version_deprecates_previous_version()
    {
        var actorId = Guid.NewGuid();
        var playbook = CreatePlaybook(actorId);
        var first = playbook.Versions.Single();
        playbook.Publish(first.Id, actorId, DateTimeOffset.UtcNow);
        var secondId = playbook.CreateDraft(actorId, DateTimeOffset.UtcNow);

        playbook.Publish(secondId, actorId, DateTimeOffset.UtcNow);

        Assert.Equal(PlaybookVersionStatus.Deprecated, first.Status);
        var second = playbook.Versions.Single(version => version.Id == secondId);
        Assert.Equal(2, second.Version);
        Assert.Equal(PlaybookVersionStatus.Published, second.Status);
        Assert.Equal(PlaybookStatus.Active, playbook.Status);
    }

    private static Playbook CreatePlaybook(Guid actorId) =>
        Playbook.Create(
            Guid.NewGuid(),
            InitialPlaybooks.PropertyClaimIntakeKey,
            "Property Claim Intake",
            "Initial public adjusting workflow.",
            "Primary",
            1,
            InitialPlaybooks.PropertyClaimIntakeStages(),
            actorId,
            DateTimeOffset.UtcNow);
}
