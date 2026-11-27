using NUnit.Framework;
using Domain.Attendance;

namespace Tests.Domain.Attendance;

[TestFixture]
public sealed class GroupMembershipTests
{
    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid OfferingId = Guid.NewGuid();
    private static readonly Guid GroupId = Guid.NewGuid();

    [Test]
    public void Create_InGroup_KeepsData()
    {
        var membership = new GroupMembership(
            StudentId, OfferingId, GroupId, MembershipPeriod.Always);

        Assert.Multiple(() =>
        {
            Assert.That(membership.StudentId, Is.EqualTo(StudentId));
            Assert.That(membership.OfferingId, Is.EqualTo(OfferingId));
            Assert.That(membership.GroupId, Is.EqualTo(GroupId));
        });
    }

    [Test]
    public void Create_WithoutGroup_IsAllowed()
    {
        var membership = new GroupMembership(
            StudentId, OfferingId, null, MembershipPeriod.Always);

        Assert.That(membership.GroupId, Is.Null);
    }

    [Test]
    public void Create_EmptyStudent_Throws()
    {
        Assert.Throws<ArgumentException>(() => new GroupMembership(
            Guid.Empty, OfferingId, GroupId, MembershipPeriod.Always));
    }

    [Test]
    public void Create_EmptyOffering_Throws()
    {
        Assert.Throws<ArgumentException>(() => new GroupMembership(
            StudentId, Guid.Empty, GroupId, MembershipPeriod.Always));
    }

    [Test]
    public void Create_EmptyGroup_Throws()
    {
        Assert.Throws<ArgumentException>(() => new GroupMembership(
            StudentId, OfferingId, Guid.Empty, MembershipPeriod.Always));
    }

    [Test]
    public void Create_MissingPeriod_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new GroupMembership(
            StudentId, OfferingId, GroupId, null!));
    }
}
