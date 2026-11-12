using NUnit.Framework;
using Application.TeachingSessions;
using Domain.TeachingSessions;
using Infrastructure.TeachingSessions;

namespace Tests.Application;

[TestFixture]
public sealed class SearchSessionsHandlerTests
{
    private static readonly Guid OfferingId = Guid.NewGuid();
    private static readonly Guid OtherOfferingId = Guid.NewGuid();
    private static readonly DateOnly Date = new(2026, 11, 7);

    private static TeachingSession Session(
        Guid offeringId,
        DateTime startsAt,
        SessionState state)
    {
        var session = new TeachingSession(
            Guid.NewGuid(), "Vežbe", offeringId, Guid.NewGuid(),
            startsAt, startsAt.AddMinutes(90), "Učionica 101");
        if (state == SessionState.Odrzan)
            session.MarkHeld();
        else if (state == SessionState.Otkazan)
            session.Cancel();
        return session;
    }

    [Test]
    public async Task Handle_SplitsActiveFromHistory()
    {
        var repository = new InMemorySessionRepository();
        await repository.AddAsync(Session(OfferingId,
            new DateTime(2026, 11, 7, 10, 0, 0), SessionState.Planiran));
        await repository.AddAsync(Session(OfferingId,
            new DateTime(2026, 11, 7, 12, 0, 0), SessionState.Odrzan));
        await repository.AddAsync(Session(OfferingId,
            new DateTime(2026, 11, 7, 14, 0, 0), SessionState.Otkazan));

        var result = await new SearchSessionsHandler(repository)
            .HandleAsync(OfferingId, Date);

        Assert.Multiple(() =>
        {
            Assert.That(result.Active, Has.Count.EqualTo(2));
            Assert.That(result.History, Has.Count.EqualTo(1));
            Assert.That(result.History[0].State, Is.EqualTo(SessionState.Otkazan));
        });
    }

    [Test]
    public async Task Handle_ExcludesOtherOfferingAndDate()
    {
        var repository = new InMemorySessionRepository();
        await repository.AddAsync(Session(OfferingId,
            new DateTime(2026, 11, 7, 10, 0, 0), SessionState.Planiran));
        await repository.AddAsync(Session(OtherOfferingId,
            new DateTime(2026, 11, 7, 10, 0, 0), SessionState.Planiran));
        await repository.AddAsync(Session(OfferingId,
            new DateTime(2026, 11, 8, 10, 0, 0), SessionState.Planiran));

        var result = await new SearchSessionsHandler(repository)
            .HandleAsync(OfferingId, Date);

        Assert.Multiple(() =>
        {
            Assert.That(result.Active, Has.Count.EqualTo(1));
            Assert.That(result.History, Is.Empty);
        });
    }
}
