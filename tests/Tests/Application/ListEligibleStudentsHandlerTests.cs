using NUnit.Framework;
using Application.Attendance;
using Domain.Attendance;
using Domain.TeachingSessions;
using Infrastructure.Attendance;

namespace Tests.Application;

[TestFixture]
public sealed class ListEligibleStudentsHandlerTests
{
    private static readonly Guid OfferingId = Guid.NewGuid();
    private static readonly Guid GroupId = Guid.NewGuid();

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

    [Test]
    public async Task Handle_TwentyEligible_EighteenRecorded_TwoNeevidentirano()
    {
        var session = HeldSession();
        var students = Enumerable.Range(0, 20).Select(_ => Guid.NewGuid()).ToArray();
        var eligibility = new InMemoryEligibilityProvider();
        foreach (var student in students)
            eligibility.Add(OfferingId, GroupId, student);

        var attendance = new InMemoryAttendanceRepository();
        var actor = Guid.NewGuid();
        var record = new RecordAttendanceHandler(eligibility, attendance);
        foreach (var student in students.Take(18))
            await record.HandleAsync(session, student, AttendanceState.Prisutan, actor);

        var list = new ListEligibleStudentsHandler(eligibility, attendance);
        var result = await list.HandleAsync(session);

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(20));
            Assert.That(
                result.Count(e => e.State == AttendanceState.Prisutan),
                Is.EqualTo(18));
            Assert.That(
                result.Count(e => e.State == AttendanceState.Neevidentirano),
                Is.EqualTo(2));
        });
    }

    [Test]
    public void Handle_OnCancelledSession_Throws()
    {
        var session = new TeachingSession(
            Guid.NewGuid(), "Vežbe", OfferingId, GroupId,
            new DateTime(2026, 11, 7, 10, 0, 0),
            new DateTime(2026, 11, 7, 11, 30, 0),
            "Učionica 101");
        session.Cancel();
        var list = new ListEligibleStudentsHandler(
            new InMemoryEligibilityProvider(), new InMemoryAttendanceRepository());

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await list.HandleAsync(session));
    }
}
