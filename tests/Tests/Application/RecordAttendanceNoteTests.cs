using NUnit.Framework;
using Application.Attendance;
using Domain.Attendance;
using Domain.TeachingSessions;
using Infrastructure.Attendance;

namespace Tests.Application;

[TestFixture]
public sealed class RecordAttendanceNoteTests
{
    private static readonly Guid OfferingId = Guid.NewGuid();
    private static readonly Guid GroupId = Guid.NewGuid();
    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();

    [Test]
    public async Task Handle_WithNote_StoresNote()
    {
        var session = new TeachingSession(
            Guid.NewGuid(), "Vežbe", OfferingId, GroupId,
            new DateTime(2026, 11, 7, 10, 0, 0),
            new DateTime(2026, 11, 7, 11, 30, 0),
            "Učionica 101");
        session.MarkHeld();
        var eligibility = new InMemoryEligibilityProvider()
            .Add(OfferingId, GroupId, StudentId);
        var attendance = new InMemoryAttendanceRepository();

        await new RecordAttendanceHandler(eligibility, attendance).HandleAsync(
            session, StudentId, AttendanceState.Prisutan, ActorId, "Bolesna");

        var stored = await attendance.GetAsync(session.Id);
        Assert.That(stored, Is.Not.Null);
        Assert.That(stored!.Records[0].Note, Is.EqualTo("Bolesna"));
    }
}
