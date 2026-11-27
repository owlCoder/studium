using NUnit.Framework;
using Application.TeachingSessions;
using Infrastructure.TeachingSessions;

namespace Tests.Application.TeachingSessions;

/// <summary>
/// Story "Čuvanje termina": odbijen termin se ne čuva.
/// </summary>
[TestFixture]
public sealed class CreateSessionRejectionTests
{
    private static readonly Guid OfferingId = Guid.NewGuid();
    private static readonly Guid V1 = Guid.NewGuid();
    private static readonly DateOnly Day = new(2026, 11, 7);

    private static InMemoryCourseOfferingReader Offerings() =>
        new InMemoryCourseOfferingReader().AddOffering(OfferingId).AddGroup(OfferingId, V1);

    private static async Task<int> StoredSessionCount(InMemorySessionRepository sessions) =>
        (await sessions.ListByOfferingAndDateAsync(OfferingId, Day)).Count;

    [Test]
    public async Task EndAt0930_IsRejectedAndNotStored()
    {
        var sessions = new InMemorySessionRepository();
        var handler = new CreateSessionHandler(Offerings(), sessions);

        Assert.ThrowsAsync<ArgumentException>(async () => await handler.HandleAsync(
            new CreateSessionCommand(
                "Vežbe", OfferingId, V1,
                new DateTime(2026, 11, 7, 10, 0, 0),
                new DateTime(2026, 11, 7, 9, 30, 0),
                "Učionica 101")));

        Assert.That(await StoredSessionCount(sessions), Is.Zero);
    }

    [Test]
    public async Task ForeignGroup_IsRejectedAndNotStored()
    {
        var sessions = new InMemorySessionRepository();
        var handler = new CreateSessionHandler(
            new InMemoryCourseOfferingReader().AddOffering(OfferingId), sessions);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await handler.HandleAsync(
            new CreateSessionCommand(
                "Vežbe", OfferingId, V1,
                new DateTime(2026, 11, 7, 10, 0, 0),
                new DateTime(2026, 11, 7, 11, 30, 0),
                "Učionica 101")));

        Assert.That(await StoredSessionCount(sessions), Is.Zero);
    }

    [Test]
    public async Task MissingKind_IsRejectedAndNotStored()
    {
        var sessions = new InMemorySessionRepository();
        var handler = new CreateSessionHandler(Offerings(), sessions);

        Assert.ThrowsAsync<ArgumentException>(async () => await handler.HandleAsync(
            new CreateSessionCommand(
                "   ", OfferingId, V1,
                new DateTime(2026, 11, 7, 10, 0, 0),
                new DateTime(2026, 11, 7, 11, 30, 0),
                "Učionica 101")));

        Assert.That(await StoredSessionCount(sessions), Is.Zero);
    }
}
