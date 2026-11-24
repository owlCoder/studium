using NUnit.Framework;
using Studium.Domain.Entities;

namespace Tests.Domain;

[TestFixture]
public sealed class TeamTests
{
    private static readonly Guid Offering = Guid.NewGuid();
    private static readonly Guid Activity = Guid.NewGuid();
    private static readonly Guid Creator = Guid.NewGuid();

    [Test]
    public void TryCreate_AssignsProperties()
    {
        var creation = StudentTeam.TryCreate(Offering, Activity, "Tim A", Creator, 2, 5);

        Assert.That(creation.IsSuccess, Is.True);
        var team = creation.Team!;
        Assert.Multiple(() =>
        {
            Assert.That(team.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(team.CourseOfferingId, Is.EqualTo(Offering));
            Assert.That(team.ProjectActivityId, Is.EqualTo(Activity));
            Assert.That(team.Name, Is.EqualTo("Tim A"));
            Assert.That(team.MinSize, Is.EqualTo(2));
            Assert.That(team.MaxSize, Is.EqualTo(5));
            Assert.That(team.MemberStudentIds, Is.EqualTo(new List<Guid> { Creator }));
            Assert.That(team.PendingInvitationStudentIds, Is.Empty);
            Assert.That(team.Status, Is.EqualTo(TeamCompositionStatus.Open));
        });
    }

    [Test]
    public void TryCreate_TrimsName()
    {
        var creation = StudentTeam.TryCreate(Offering, Activity, "  Tim A  ", Creator, 2, 5);

        Assert.That(creation.IsSuccess, Is.True);
        Assert.That(creation.Team!.Name, Is.EqualTo("Tim A"));
    }

    [Test]
    public void TryCreate_RejectsEmptyCourseOfferingId()
    {
        var creation = StudentTeam.TryCreate(Guid.Empty, Activity, "Tim A", Creator, 2, 5);

        AssertFailure(creation);
    }

    [Test]
    public void TryCreate_RejectsEmptyProjectActivityId()
    {
        var creation = StudentTeam.TryCreate(Offering, Guid.Empty, "Tim A", Creator, 2, 5);

        AssertFailure(creation);
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void TryCreate_RejectsBlankName(string? name)
    {
        var creation = StudentTeam.TryCreate(Offering, Activity, name, Creator, 2, 5);

        AssertFailure(creation);
    }

    [Test]
    public void TryCreate_RejectsEmptyCreator()
    {
        var creation = StudentTeam.TryCreate(Offering, Activity, "Tim A", Guid.Empty, 2, 5);

        AssertFailure(creation);
    }

    [TestCase(0)]
    [TestCase(-3)]
    public void TryCreate_RejectsMinSizeBelowOne(int minSize)
    {
        var creation = StudentTeam.TryCreate(Offering, Activity, "Tim A", Creator, minSize, 5);

        AssertFailure(creation);
    }

    [Test]
    public void TryCreate_RejectsMaxSizeBelowMinSize()
    {
        var creation = StudentTeam.TryCreate(Offering, Activity, "Tim A", Creator, 4, 3);

        AssertFailure(creation);
    }

    [Test]
    public void TryCreate_AcceptsMaxSizeEqualToMinSize()
    {
        var creation = StudentTeam.TryCreate(Offering, Activity, "Tim A", Creator, 3, 3);

        Assert.That(creation.IsSuccess, Is.True);
        Assert.That(creation.Team!.MinSize, Is.EqualTo(3));
        Assert.That(creation.Team.MaxSize, Is.EqualTo(3));
    }

    private static void AssertFailure(TeamCreation creation)
    {
        Assert.Multiple(() =>
        {
            Assert.That(creation.IsSuccess, Is.False);
            Assert.That(creation.Team, Is.Null);
            Assert.That(creation.Error, Is.Not.Null.And.Not.Empty);
        });
    }
    [Test]
    public void StudentTeam_ExposesNoPublicSetters()
    {
        var writable = typeof(StudentTeam)
            .GetProperties()
            .Where(property => property.SetMethod is { IsPublic: true })
            .Select(property => property.Name)
            .ToArray();

        Assert.That(writable, Is.Empty);
    }

    [Test]
    public void MemberStudentIds_IsReadOnlyView()
    {
        var team = StudentTeam.TryCreate(Offering, Activity, "Tim A", Creator, 2, 5).Team!;

        var members = team.MemberStudentIds;

        Assert.Multiple(() =>
        {
            Assert.That(members, Is.EqualTo(new[] { Creator }));
            Assert.That(((ICollection<Guid>)members).IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => ((ICollection<Guid>)members).Add(Guid.NewGuid()));
            Assert.That(team.MemberStudentIds, Is.EqualTo(new[] { Creator }));
        });
    }

    [Test]
    public void PendingInvitationStudentIds_IsReadOnlyView()
    {
        var team = StudentTeam.TryCreate(Offering, Activity, "Tim A", Creator, 2, 5).Team!;

        var invitations = team.PendingInvitationStudentIds;

        Assert.Multiple(() =>
        {
            Assert.That(invitations, Is.Empty);
            Assert.That(((ICollection<Guid>)invitations).IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => ((ICollection<Guid>)invitations).Add(Guid.NewGuid()));
        });
    }

    [Test]
    public void MemberStudentIds_ViewCannotBeMutatedThroughReturnedCollection()
    {
        var team = StudentTeam.TryCreate(Offering, Activity, "Tim A", Creator, 2, 5).Team!;
        var stranger = Guid.NewGuid();

        Assert.Throws<NotSupportedException>(() => ((IList<Guid>)team.MemberStudentIds).Add(stranger));
        Assert.That(team.MemberStudentIds, Does.Not.Contain(stranger));
    }
}
