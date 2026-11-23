using NUnit.Framework;
using Domain.Attendance;

namespace Tests.Domain.Attendance;

[TestFixture]
public sealed class EligibilityRuleWholeOfferingTests
{
    private static readonly Guid OfferingId = Guid.NewGuid();
    private static readonly Guid V1 = Guid.NewGuid();
    private static readonly Guid V2 = Guid.NewGuid();
    private static readonly DateTime SessionStart = new(2026, 11, 7, 10, 0, 0);

    [Test]
    public void WholeOffering_IncludesMembersOfAllGroups()
    {
        var ana = Guid.NewGuid();
        var marko = Guid.NewGuid();
        var memberships = new[]
        {
            new GroupMembership(ana, OfferingId, V1, MembershipPeriod.Always),
            new GroupMembership(marko, OfferingId, V2, MembershipPeriod.Always),
        };

        var result = EligibilityRule.EligibleStudents(memberships, OfferingId, null, SessionStart);

        Assert.That(result, Is.EquivalentTo(new[] { ana, marko }));
    }

    [Test]
    public void WholeOffering_IncludesMembersWithoutGroup()
    {
        var ana = Guid.NewGuid();
        var memberships = new[]
        {
            new GroupMembership(ana, OfferingId, null, MembershipPeriod.Always),
        };

        var result = EligibilityRule.EligibleStudents(memberships, OfferingId, null, SessionStart);

        Assert.That(result, Is.EqualTo(new[] { ana }));
    }

    [Test]
    public void WholeOffering_ReturnsStudentOnlyOnce()
    {
        // Student je na izvođenju i bez grupe i u grupi V1 u isto vreme.
        var ana = Guid.NewGuid();
        var memberships = new[]
        {
            new GroupMembership(ana, OfferingId, null, MembershipPeriod.Always),
            new GroupMembership(ana, OfferingId, V1, MembershipPeriod.Always),
        };

        var result = EligibilityRule.EligibleStudents(memberships, OfferingId, null, SessionStart);

        Assert.That(result, Is.EqualTo(new[] { ana }));
    }

    [Test]
    public void WholeOffering_StillRespectsPeriod()
    {
        var ana = Guid.NewGuid();
        var marko = Guid.NewGuid();
        var memberships = new[]
        {
            new GroupMembership(ana, OfferingId, V1, MembershipPeriod.Always),
            new GroupMembership(marko, OfferingId, V2,
                new MembershipPeriod(SessionStart.AddDays(3))),
        };

        var result = EligibilityRule.EligibleStudents(memberships, OfferingId, null, SessionStart);

        Assert.That(result, Is.EqualTo(new[] { ana }));
    }
}
