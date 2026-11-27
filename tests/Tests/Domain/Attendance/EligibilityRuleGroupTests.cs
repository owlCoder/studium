using NUnit.Framework;
using Domain.Attendance;

namespace Tests.Domain.Attendance;

[TestFixture]
public sealed class EligibilityRuleGroupTests
{
    private static readonly Guid OfferingId = Guid.NewGuid();
    private static readonly Guid OtherOfferingId = Guid.NewGuid();
    private static readonly Guid V1 = Guid.NewGuid();
    private static readonly Guid V2 = Guid.NewGuid();
    private static readonly DateTime SessionStart = new(2026, 11, 7, 10, 0, 0);

    private static GroupMembership Member(Guid student, Guid offering, Guid? group, MembershipPeriod? period = null) =>
        new(student, offering, group, period ?? MembershipPeriod.Always);

    [Test]
    public void GroupSession_ReturnsOnlyMembersOfThatGroup()
    {
        var ana = Guid.NewGuid();
        var marko = Guid.NewGuid();
        var memberships = new[]
        {
            Member(ana, OfferingId, V1),
            Member(marko, OfferingId, V2),
        };

        var result = EligibilityRule.EligibleStudents(memberships, OfferingId, V1, SessionStart);

        Assert.That(result, Is.EqualTo(new[] { ana }));
    }

    [Test]
    public void GroupSession_IgnoresSameGroupIdOnOtherOffering()
    {
        var ana = Guid.NewGuid();
        var memberships = new[] { Member(ana, OtherOfferingId, V1) };

        var result = EligibilityRule.EligibleStudents(memberships, OfferingId, V1, SessionStart);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GroupSession_ExcludesMembershipThatStartsAfterSession()
    {
        var ana = Guid.NewGuid();
        var memberships = new[]
        {
            Member(ana, OfferingId, V1, new MembershipPeriod(SessionStart.AddDays(1))),
        };

        var result = EligibilityRule.EligibleStudents(memberships, OfferingId, V1, SessionStart);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GroupSession_ExcludesMembershipThatEndedBeforeSession()
    {
        var ana = Guid.NewGuid();
        var memberships = new[]
        {
            Member(ana, OfferingId, V1, new MembershipPeriod(
                new DateTime(2026, 10, 1), SessionStart.AddDays(-1))),
        };

        var result = EligibilityRule.EligibleStudents(memberships, OfferingId, V1, SessionStart);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GroupSession_StudentWithoutGroupIsNotIncluded()
    {
        var ana = Guid.NewGuid();
        var memberships = new[] { Member(ana, OfferingId, null) };

        var result = EligibilityRule.EligibleStudents(memberships, OfferingId, V1, SessionStart);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void NoMemberships_ReturnsEmptyList()
    {
        var result = EligibilityRule.EligibleStudents(
            Array.Empty<GroupMembership>(), OfferingId, V1, SessionStart);

        Assert.That(result, Is.Empty);
    }
}
