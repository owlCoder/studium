using NUnit.Framework;
using Application.Attendance;
using Domain.Attendance;
using Domain.TeachingSessions;
using Infrastructure.Attendance;

namespace Tests.Application.Attendance;

/// <summary>
/// Termin za celo izvođenje prikazuje studente iz svih grupa.
/// </summary>
[TestFixture]
public sealed class EligibilityWholeOfferingTests
{
    private static readonly Guid OfferingId = Guid.NewGuid();
    private static readonly Guid V1 = Guid.NewGuid();
    private static readonly Guid V2 = Guid.NewGuid();

    private static TeachingSession HeldLectureForWholeOffering()
    {
        var session = new TeachingSession(
            Guid.NewGuid(), "Predavanje", OfferingId, null,
            new DateTime(2026, 11, 7, 10, 0, 0),
            new DateTime(2026, 11, 7, 11, 30, 0),
            "Amfiteatar");
        session.MarkHeld();
        return session;
    }

    [Test]
    public async Task WholeOfferingSession_ShowsStudentsFromTwoGroups()
    {
        var ana = Guid.NewGuid();
        var marko = Guid.NewGuid();
        var eligibility = new InMemoryEligibilityProvider()
            .Add(OfferingId, V1, ana)
            .Add(OfferingId, V2, marko);
        var list = new ListEligibleStudentsHandler(eligibility, new InMemoryAttendanceRepository());

        var result = await list.HandleAsync(HeldLectureForWholeOffering());

        Assert.Multiple(() =>
        {
            Assert.That(result.Select(s => s.StudentId), Is.EquivalentTo(new[] { ana, marko }));
            Assert.That(result.All(s => s.State == AttendanceState.Neevidentirano), Is.True);
        });
    }

    [Test]
    public async Task WholeOfferingSession_AcceptsAttendanceForStudentFromAnyGroup()
    {
        var marko = Guid.NewGuid();
        var eligibility = new InMemoryEligibilityProvider().Add(OfferingId, V2, marko);
        var attendance = new InMemoryAttendanceRepository();
        var session = HeldLectureForWholeOffering();

        await new RecordAttendanceHandler(eligibility, attendance)
            .HandleAsync(session, marko, AttendanceState.Prisutan, Guid.NewGuid());

        var stored = await attendance.GetAsync(session.Id);
        Assert.That(stored!.StateOf(marko), Is.EqualTo(AttendanceState.Prisutan));
    }
}
