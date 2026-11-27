using NUnit.Framework;
using Application.Attendance;
using Domain.Attendance;
using Domain.TeachingSessions;
using Infrastructure.Attendance;

namespace Tests.Application.Attendance;

/// <summary>
/// Granice perioda članstva u odnosu na početak termina.
/// Članstvo koje počinje tačno u vreme termina se računa, a članstvo koje
/// prestaje tačno u vreme termina se ne računa. Asistent je potvrdio da se
/// student prebačen u drugu grupu odmah računa u novoj grupi (ADR-0005).
/// </summary>
[TestFixture]
public sealed class EligibilityBoundaryTests
{
    private static readonly Guid OfferingId = Guid.NewGuid();
    private static readonly Guid V1 = Guid.NewGuid();
    private static readonly DateTime SessionStart = new(2026, 11, 7, 10, 0, 0);

    private static TeachingSession HeldSession()
    {
        var session = new TeachingSession(
            Guid.NewGuid(), "Vežbe", OfferingId, V1,
            SessionStart, SessionStart.AddMinutes(90), "Učionica 101");
        session.MarkHeld();
        return session;
    }

    private static async Task<IEnumerable<Guid>> EligibleWith(MembershipPeriod period, Guid student)
    {
        var eligibility = new InMemoryEligibilityProvider().Add(OfferingId, V1, student, period);
        var list = new ListEligibleStudentsHandler(eligibility, new InMemoryAttendanceRepository());
        var result = await list.HandleAsync(HeldSession());
        return result.Select(s => s.StudentId);
    }

    [Test]
    public async Task MembershipStartingExactlyAtSessionStart_IsEligible()
    {
        var ana = Guid.NewGuid();

        var result = await EligibleWith(new MembershipPeriod(SessionStart), ana);

        Assert.That(result, Does.Contain(ana));
    }

    [Test]
    public async Task MembershipEndingExactlyAtSessionStart_IsNotEligible()
    {
        var ana = Guid.NewGuid();

        var result = await EligibleWith(
            new MembershipPeriod(new DateTime(2026, 10, 1), SessionStart), ana);

        Assert.That(result, Does.Not.Contain(ana));
    }
}
