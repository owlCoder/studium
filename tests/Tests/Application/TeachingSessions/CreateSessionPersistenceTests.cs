using NUnit.Framework;
using Application.TeachingSessions;
using Domain.TeachingSessions;
using Infrastructure.TeachingSessions;

namespace Tests.Application.TeachingSessions;

/// <summary>
/// Story "Čuvanje termina": termin koji prođe provere ostaje sačuvan i pronalazi se.
/// </summary>
[TestFixture]
public sealed class CreateSessionPersistenceTests
{
    private static readonly Guid OfferingId = Guid.NewGuid();
    private static readonly Guid V1 = Guid.NewGuid();

    private static InMemoryCourseOfferingReader Offerings() =>
        new InMemoryCourseOfferingReader().AddOffering(OfferingId).AddGroup(OfferingId, V1);

    private static CreateSessionCommand VezbeV1() => new(
        "Vežbe", OfferingId, V1,
        new DateTime(2026, 11, 7, 10, 0, 0),
        new DateTime(2026, 11, 7, 11, 30, 0),
        "Učionica 101");

    [Test]
    public async Task CreatedSession_IsFoundBySearchByDateAndOffering()
    {
        var sessions = new InMemorySessionRepository();
        var created = await new CreateSessionHandler(Offerings(), sessions).HandleAsync(VezbeV1());

        var found = await new SearchSessionsHandler(sessions)
            .HandleAsync(OfferingId, new DateOnly(2026, 11, 7));

        Assert.Multiple(() =>
        {
            Assert.That(found.Active.Select(s => s.Id), Is.EqualTo(new[] { created.Id }));
            Assert.That(found.Active[0].State, Is.EqualTo(SessionState.Planiran));
        });
    }

    [Test]
    public async Task CreatedSession_ReturnsIdentifierUnderWhichItIsStored()
    {
        var sessions = new InMemorySessionRepository();

        var created = await new CreateSessionHandler(Offerings(), sessions).HandleAsync(VezbeV1());
        var stored = await sessions.GetByIdAsync(created.Id);

        Assert.Multiple(() =>
        {
            Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(stored, Is.SameAs(created));
        });
    }

    [Test]
    public async Task TwoCreatedSessions_GetDifferentIdentifiers()
    {
        var handler = new CreateSessionHandler(Offerings(), new InMemorySessionRepository());

        var first = await handler.HandleAsync(VezbeV1());
        var second = await handler.HandleAsync(VezbeV1());

        Assert.That(first.Id, Is.Not.EqualTo(second.Id));
    }

    [Test]
    public async Task SessionOnOtherDate_IsNotFound()
    {
        var sessions = new InMemorySessionRepository();
        await new CreateSessionHandler(Offerings(), sessions).HandleAsync(VezbeV1());

        var found = await new SearchSessionsHandler(sessions)
            .HandleAsync(OfferingId, new DateOnly(2026, 11, 8));

        Assert.That(found.Active, Is.Empty);
    }
}
