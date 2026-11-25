using NUnit.Framework;
using Application.Attendance;
using Application.TeachingSessions;
using Domain.Attendance;
using Domain.TeachingSessions;
using Infrastructure.Attendance;
using Infrastructure.TeachingSessions;

namespace Tests.Application.TeachingSessions;

/// <summary>
/// Story "Otkazan termin": otkazan termin ostaje u istoriji i ne prima nov unos prisustva.
/// </summary>
[TestFixture]
public sealed class CancelledSessionHistoryTests
{
    private static readonly Guid OfferingId = Guid.NewGuid();
    private static readonly Guid GroupId = Guid.NewGuid();
    private static readonly DateOnly Day = new(2026, 11, 7);

    private static TeachingSession NewSession() => new(
        Guid.NewGuid(), "Vežbe", OfferingId, GroupId,
        new DateTime(2026, 11, 7, 10, 0, 0),
        new DateTime(2026, 11, 7, 11, 30, 0),
        "Učionica 101");

    [Test]
    public async Task CancelledSession_IsInHistoryNotAmongActive()
    {
        var sessions = new InMemorySessionRepository();
        var session = NewSession();
        await sessions.AddAsync(session);

        await new CancelSessionHandler(sessions).HandleAsync(session.Id);
        var result = await new SearchSessionsHandler(sessions).HandleAsync(OfferingId, Day);

        Assert.Multiple(() =>
        {
            Assert.That(result.Active, Is.Empty);
            Assert.That(result.History.Select(s => s.Id), Is.EqualTo(new[] { session.Id }));
        });
    }

    [Test]
    public async Task CancelledSession_RejectsNewAttendance()
    {
        var ana = Guid.NewGuid();
        var sessions = new InMemorySessionRepository();
        var session = NewSession();
        await sessions.AddAsync(session);
        var cancelled = await new CancelSessionHandler(sessions).HandleAsync(session.Id);
        var attendance = new InMemoryAttendanceRepository();
        var record = new RecordAttendanceHandler(
            new InMemoryEligibilityProvider().Add(OfferingId, GroupId, ana), attendance);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await record.HandleAsync(
            cancelled, ana, AttendanceState.Prisutan, Guid.NewGuid()));

        Assert.That(await attendance.GetAsync(session.Id), Is.Null);
    }

    [Test]
    public async Task CancellingOneSession_DoesNotAffectOtherSessionSameDay()
    {
        var sessions = new InMemorySessionRepository();
        var cancelled = NewSession();
        var other = NewSession();
        await sessions.AddAsync(cancelled);
        await sessions.AddAsync(other);

        await new CancelSessionHandler(sessions).HandleAsync(cancelled.Id);
        var result = await new SearchSessionsHandler(sessions).HandleAsync(OfferingId, Day);

        Assert.That(result.Active.Select(s => s.Id), Is.EqualTo(new[] { other.Id }));
    }
}
