using NUnit.Framework;
using Application.Attendance;
using Application.TeachingSessions;
using Domain.Attendance;
using Domain.TeachingSessions;
using Infrastructure.Attendance;
using Infrastructure.TeachingSessions;

namespace Tests.Application.TeachingSessions;

[TestFixture]
public sealed class HeldSessionAttendanceTests
{
    private static readonly Guid OfferingId = Guid.NewGuid();
    private static readonly Guid GroupId = Guid.NewGuid();
    private static readonly Guid Ana = Guid.NewGuid();

    private static TeachingSession NewSession() => new(
        Guid.NewGuid(), "Vežbe", OfferingId, GroupId,
        new DateTime(2026, 11, 7, 10, 0, 0),
        new DateTime(2026, 11, 7, 11, 30, 0),
        "Učionica 101");

    private static RecordAttendanceHandler RecordHandler(IAttendanceRepository attendance) =>
        new(new InMemoryEligibilityProvider().Add(OfferingId, GroupId, Ana), attendance);

    [Test]
    public async Task HeldSession_AcceptsAttendance()
    {
        var sessions = new InMemorySessionRepository();
        var session = NewSession();
        await sessions.AddAsync(session);
        var held = await new MarkSessionHeldHandler(sessions).HandleAsync(session.Id);
        var attendance = new InMemoryAttendanceRepository();

        await RecordHandler(attendance).HandleAsync(
            held, Ana, AttendanceState.Prisutan, Guid.NewGuid());

        var stored = await attendance.GetAsync(session.Id);
        Assert.That(stored!.StateOf(Ana), Is.EqualTo(AttendanceState.Prisutan));
    }

    [Test]
    public async Task PlannedSession_RejectsAttendance()
    {
        var sessions = new InMemorySessionRepository();
        var session = NewSession();
        await sessions.AddAsync(session);
        var attendance = new InMemoryAttendanceRepository();

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await RecordHandler(attendance).HandleAsync(
                session, Ana, AttendanceState.Prisutan, Guid.NewGuid()));

        Assert.That(await attendance.GetAsync(session.Id), Is.Null);
    }
}
