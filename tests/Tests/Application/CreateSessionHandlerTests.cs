using NUnit.Framework;
using Application.TeachingSessions;
using Domain.TeachingSessions;
using Infrastructure.TeachingSessions;

namespace Tests.Application;

[TestFixture]
public sealed class CreateSessionHandlerTests
{
    private static readonly Guid OfferingId = Guid.NewGuid();
    private static readonly Guid GroupId = Guid.NewGuid();
    private static readonly DateTime StartsAt = new(2026, 11, 7, 10, 0, 0);
    private static readonly DateTime EndsAt = new(2026, 11, 7, 11, 30, 0);

    private static CreateSessionHandler Handler(ICourseOfferingReader offerings) => new(offerings);

    [Test]
    public async Task Handle_WholeOfferingTarget_CreatesPlannedSession()
    {
        var offerings = new InMemoryCourseOfferingReader().AddOffering(OfferingId);

        var session = await Handler(offerings).HandleAsync(new CreateSessionCommand(
            "Predavanje", OfferingId, null, StartsAt, EndsAt, "Amfiteatar"));

        Assert.Multiple(() =>
        {
            Assert.That(session.State, Is.EqualTo(SessionState.Planiran));
            Assert.That(session.OfferingId, Is.EqualTo(OfferingId));
            Assert.That(session.TargetsWholeOffering, Is.True);
        });
    }

    [Test]
    public async Task Handle_ValidGroup_CreatesSessionForThatGroup()
    {
        var offerings = new InMemoryCourseOfferingReader()
            .AddOffering(OfferingId)
            .AddGroup(OfferingId, GroupId);

        var session = await Handler(offerings).HandleAsync(new CreateSessionCommand(
            "Vežbe", OfferingId, GroupId, StartsAt, EndsAt, "Učionica 101"));

        Assert.That(session.GroupId, Is.EqualTo(GroupId));
    }

    [Test]
    public void Handle_ForeignGroup_Throws()
    {
        var offerings = new InMemoryCourseOfferingReader().AddOffering(OfferingId);

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await Handler(offerings).HandleAsync(new CreateSessionCommand(
                "Vežbe", OfferingId, GroupId, StartsAt, EndsAt, "Učionica 101")));
    }

    [Test]
    public void Handle_MissingOffering_Throws()
    {
        var offerings = new InMemoryCourseOfferingReader();

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await Handler(offerings).HandleAsync(new CreateSessionCommand(
                "Vežbe", OfferingId, GroupId, StartsAt, EndsAt, "Učionica 101")));
    }
}
