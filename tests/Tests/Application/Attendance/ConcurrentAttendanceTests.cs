using NUnit.Framework;
using Application.Attendance;
using Domain.Attendance;
using Domain.TeachingSessions;
using Infrastructure.Attendance;

namespace Tests.Application.Attendance;

/// <summary>
/// Story "Jedan zapis po studentu i terminu": istovremeni unosi ne smeju
/// napraviti dva zapisa niti izgubiti nečiji unos.
/// </summary>
[TestFixture]
public sealed class ConcurrentAttendanceTests
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
    public async Task ManyConcurrentEntriesForSameStudent_LeaveExactlyOneRecord()
    {
        var ana = Guid.NewGuid();
        var session = HeldSession();
        var attendance = new InMemoryAttendanceRepository();
        var handler = new RecordAttendanceHandler(
            new InMemoryEligibilityProvider().Add(OfferingId, GroupId, ana), attendance);

        var entries = Enumerable.Range(0, 50).Select(i => Task.Run(() => handler.HandleAsync(
            session, ana,
            i % 2 == 0 ? AttendanceState.Prisutan : AttendanceState.Odsutan,
            Guid.NewGuid())));
        await Task.WhenAll(entries);

        var stored = await attendance.GetAsync(session.Id);
        Assert.That(stored!.Records.Count(r => r.StudentId == ana), Is.EqualTo(1));
    }

    [Test]
    public async Task ConcurrentFirstEntriesForDifferentStudents_AreAllKept()
    {
        var students = Enumerable.Range(0, 40).Select(_ => Guid.NewGuid()).ToArray();
        var eligibility = new InMemoryEligibilityProvider();
        foreach (var student in students)
            eligibility.Add(OfferingId, GroupId, student);
        var session = HeldSession();
        var attendance = new InMemoryAttendanceRepository();
        var handler = new RecordAttendanceHandler(eligibility, attendance);

        await Task.WhenAll(students.Select(student => Task.Run(() => handler.HandleAsync(
            session, student, AttendanceState.Prisutan, Guid.NewGuid()))));

        var stored = await attendance.GetAsync(session.Id);
        Assert.That(stored!.Records, Has.Count.EqualTo(students.Length));
    }

    [Test]
    public async Task GetOrCreate_CalledConcurrently_ReturnsSameEvidence()
    {
        var sessionId = Guid.NewGuid();
        var attendance = new InMemoryAttendanceRepository();

        var results = await Task.WhenAll(Enumerable.Range(0, 20)
            .Select(_ => Task.Run(() => attendance.GetOrCreateAsync(sessionId))));

        Assert.That(results.Distinct().Count(), Is.EqualTo(1));
    }
}
