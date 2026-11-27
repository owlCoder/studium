using NUnit.Framework;
using Application.Attendance;
using Application.TeachingSessions;
using Domain.Attendance;
using Domain.TeachingSessions;
using Infrastructure.Attendance;
using Infrastructure.TeachingSessions;

namespace Tests.Acceptance;

/// <summary>
/// Testovi prihvatanja za ST-031, po primeru iz specifikacije:
/// "Vežbe za V1 traju od 10:00 do 11:30. Završetak u 09:30 se odbija.
/// Otkazivanjem termin ostaje u istoriji, ali se više ne prikazuje kao aktivna
/// obaveza i ne dozvoljava nov unos prisustva."
/// </summary>
[TestFixture]
public sealed class St031AcceptanceTests
{
    private static readonly Guid OfferingId = Guid.NewGuid();
    private static readonly Guid V1 = Guid.NewGuid();
    private static readonly Guid Ana = Guid.NewGuid();
    private static readonly DateOnly Day = new(2026, 11, 7);

    private InMemorySessionRepository _sessions = null!;
    private CreateSessionHandler _create = null!;

    [SetUp]
    public void SetUp()
    {
        _sessions = new InMemorySessionRepository();
        var offerings = new InMemoryCourseOfferingReader()
            .AddOffering(OfferingId)
            .AddGroup(OfferingId, V1);
        _create = new CreateSessionHandler(offerings, _sessions);
    }

    private static CreateSessionCommand VezbeV1(int endHour, int endMinute) => new(
        "Vežbe", OfferingId, V1,
        new DateTime(2026, 11, 7, 10, 0, 0),
        new DateTime(2026, 11, 7, endHour, endMinute, 0),
        "Učionica 101");

    [Test]
    public async Task VezbeV1_From1000To1130_AreAcceptedAsPlanned()
    {
        var session = await _create.HandleAsync(VezbeV1(11, 30));

        var result = await new SearchSessionsHandler(_sessions).HandleAsync(OfferingId, Day);

        Assert.Multiple(() =>
        {
            Assert.That(session.State, Is.EqualTo(SessionState.Planiran));
            Assert.That(result.Active.Select(s => s.Id), Is.EqualTo(new[] { session.Id }));
        });
    }

    [Test]
    public async Task VezbeV1_EndingAt0930_AreRejected()
    {
        var ex = Assert.ThrowsAsync<ArgumentException>(
            async () => await _create.HandleAsync(VezbeV1(9, 30)));

        var result = await new SearchSessionsHandler(_sessions).HandleAsync(OfferingId, Day);
        Assert.Multiple(() =>
        {
            Assert.That(ex!.Message, Does.StartWith("Završetak termina mora biti kasniji od početka."));
            Assert.That(result.Active, Is.Empty);
            Assert.That(result.History, Is.Empty);
        });
    }

    [Test]
    public async Task CancelledSession_StaysInHistory_AndRejectsNewAttendance()
    {
        var session = await _create.HandleAsync(VezbeV1(11, 30));
        var cancelled = await new CancelSessionHandler(_sessions).HandleAsync(session.Id);
        var record = new RecordAttendanceHandler(
            new InMemoryEligibilityProvider().Add(OfferingId, V1, Ana),
            new InMemoryAttendanceRepository());

        var result = await new SearchSessionsHandler(_sessions).HandleAsync(OfferingId, Day);

        Assert.Multiple(() =>
        {
            Assert.That(result.Active, Is.Empty);
            Assert.That(result.History.Select(s => s.Id), Is.EqualTo(new[] { session.Id }));
            Assert.ThrowsAsync<InvalidOperationException>(async () => await record.HandleAsync(
                cancelled, Ana, AttendanceState.Prisutan, Guid.NewGuid()));
        });
    }
}
