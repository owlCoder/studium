using NUnit.Framework;
using Application.Attendance;
using Domain.Attendance;
using Domain.TeachingSessions;
using Infrastructure.Attendance;

namespace Tests.Application.Attendance;

/// <summary>
/// Story "Podobnost studenata za termin": student koji je u grupu ušao posle
/// termina nije na spisku za taj termin.
/// </summary>
[TestFixture]
public sealed class EligibilityTimingTests
{
    private static readonly Guid OfferingId = Guid.NewGuid();
    private static readonly Guid V1 = Guid.NewGuid();
    private static readonly DateTime SessionStart = new(2026, 11, 7, 10, 0, 0);

    private static TeachingSession HeldSessionForV1()
    {
        var session = new TeachingSession(
            Guid.NewGuid(), "Vežbe", OfferingId, V1,
            SessionStart, SessionStart.AddMinutes(90), "Učionica 101");
        session.MarkHeld();
        return session;
    }

    [Test]
    public async Task StudentWhoJoinedAfterSession_IsNotOnList()
    {
        var ana = Guid.NewGuid();
        var marko = Guid.NewGuid();
        var eligibility = new InMemoryEligibilityProvider()
            .Add(OfferingId, V1, ana, new MembershipPeriod(new DateTime(2026, 10, 1)))
            .Add(OfferingId, V1, marko, new MembershipPeriod(new DateTime(2026, 11, 8)));
        var list = new ListEligibleStudentsHandler(eligibility, new InMemoryAttendanceRepository());

        var result = await list.HandleAsync(HeldSessionForV1());

        Assert.That(result.Select(s => s.StudentId), Is.EqualTo(new[] { ana }));
    }

    [Test]
    public async Task StudentWhoLeftBeforeSession_IsNotOnList()
    {
        var ana = Guid.NewGuid();
        var eligibility = new InMemoryEligibilityProvider()
            .Add(OfferingId, V1, ana, new MembershipPeriod(
                new DateTime(2026, 10, 1), new DateTime(2026, 11, 1)));
        var list = new ListEligibleStudentsHandler(eligibility, new InMemoryAttendanceRepository());

        var result = await list.HandleAsync(HeldSessionForV1());

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void StudentWhoJoinedAfterSession_CannotBeRecorded()
    {
        var marko = Guid.NewGuid();
        var eligibility = new InMemoryEligibilityProvider()
            .Add(OfferingId, V1, marko, new MembershipPeriod(new DateTime(2026, 11, 8)));
        var record = new RecordAttendanceHandler(eligibility, new InMemoryAttendanceRepository());

        Assert.ThrowsAsync<InvalidOperationException>(async () => await record.HandleAsync(
            HeldSessionForV1(), marko, AttendanceState.Prisutan, Guid.NewGuid()));
    }
}
