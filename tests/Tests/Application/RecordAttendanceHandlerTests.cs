using NUnit.Framework;
using Application.Attendance;
using Domain.Attendance;
using Domain.TeachingSessions;
using Infrastructure.Attendance;

namespace Tests.Application;

[TestFixture]
public sealed class RecordAttendanceHandlerTests
{
    private static readonly Guid OfferingId = Guid.NewGuid();
    private static readonly Guid GroupId = Guid.NewGuid();
    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();

    private static TeachingSession HeldSession()
    {
        var session = new TeachingSession(
            Guid.NewGuid(), "Vežbe", OfferingId, GroupId,
            new DateTime(2026, 11, 7, 10, 0, 0),
            new DateTime(2026, 11, 7, 11, 30, 0),
            "Učionica 101");
        session.MarkHeld();
        return session;
    }

    private static RecordAttendanceHandler Handler(
        IEligibilityProvider eligibility,
        IAttendanceRepository attendance)
        => new(eligibility, attendance);

    [Test]
    public async Task Handle_EligibleStudent_RecordsAttendance()
    {
        var session = HeldSession();
        var eligibility = new InMemoryEligibilityProvider()
            .Add(OfferingId, GroupId, StudentId);
        var attendance = new InMemoryAttendanceRepository();

        await Handler(eligibility, attendance).HandleAsync(
            session, StudentId, AttendanceState.Prisutan, ActorId);

        var stored = await attendance.GetAsync(session.Id);
        Assert.That(stored, Is.Not.Null);
        Assert.That(stored!.StateOf(StudentId), Is.EqualTo(AttendanceState.Prisutan));
    }

    [Test]
    public void Handle_IneligibleStudent_Throws()
    {
        var session = HeldSession();
        var eligibility = new InMemoryEligibilityProvider();
        var attendance = new InMemoryAttendanceRepository();

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await Handler(eligibility, attendance).HandleAsync(
                session, StudentId, AttendanceState.Prisutan, ActorId));
    }

    [Test]
    public void Handle_OnCancelledSession_Throws()
    {
        var cancelled = new TeachingSession(
            Guid.NewGuid(), "Vežbe", OfferingId, GroupId,
            new DateTime(2026, 11, 7, 10, 0, 0),
            new DateTime(2026, 11, 7, 11, 30, 0),
            "Učionica 101");
        cancelled.Cancel();
        var eligibility = new InMemoryEligibilityProvider()
            .Add(OfferingId, GroupId, StudentId);
        var attendance = new InMemoryAttendanceRepository();

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await Handler(eligibility, attendance).HandleAsync(
                cancelled, StudentId, AttendanceState.Prisutan, ActorId));
    }

    [Test]
    public async Task Handle_OnCancelledSession_KeepsExistingRecord()
    {
        var sessionId = Guid.NewGuid();
        var existing = new SessionAttendance(sessionId);
        existing.Record(StudentId, AttendanceState.Prisutan, ActorId);
        var attendance = new InMemoryAttendanceRepository();
        await attendance.SaveAsync(existing);

        var cancelled = new TeachingSession(
            sessionId, "Vežbe", OfferingId, GroupId,
            new DateTime(2026, 11, 7, 10, 0, 0),
            new DateTime(2026, 11, 7, 11, 30, 0),
            "Učionica 101");
        cancelled.Cancel();
        var otherStudent = Guid.NewGuid();
        var eligibility = new InMemoryEligibilityProvider()
            .Add(OfferingId, GroupId, otherStudent);

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await Handler(eligibility, attendance).HandleAsync(
                cancelled, otherStudent, AttendanceState.Prisutan, ActorId));

        var stored = await attendance.GetAsync(sessionId);
        Assert.That(stored, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(stored!.StateOf(StudentId), Is.EqualTo(AttendanceState.Prisutan));
            Assert.That(stored.StateOf(otherStudent), Is.EqualTo(AttendanceState.Neevidentirano));
        });
    }

    [Test]
    public async Task Handle_SecondEntry_UpdatesSameRecord()
    {
        var session = HeldSession();
        var eligibility = new InMemoryEligibilityProvider()
            .Add(OfferingId, GroupId, StudentId);
        var attendance = new InMemoryAttendanceRepository();
        var handler = Handler(eligibility, attendance);

        await handler.HandleAsync(session, StudentId, AttendanceState.Prisutan, ActorId);
        var otherActor = Guid.NewGuid();
        await handler.HandleAsync(session, StudentId, AttendanceState.Odsutan, otherActor);

        var stored = await attendance.GetAsync(session.Id);
        Assert.That(stored, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(stored!.Records, Has.Count.EqualTo(1));
            Assert.That(stored.StateOf(StudentId), Is.EqualTo(AttendanceState.Odsutan));
            Assert.That(stored.Records[0].ActorId, Is.EqualTo(otherActor));
        });
    }
}
