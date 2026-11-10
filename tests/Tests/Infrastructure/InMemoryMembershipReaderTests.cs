using NUnit.Framework;
using Application.Access;
using Infrastructure.Access;

namespace Tests.Infrastructure;

[TestFixture]
public sealed class InMemoryMembershipReaderTests
{
    private static readonly Guid Offering = Guid.NewGuid();
    private static readonly Guid OtherOffering = Guid.NewGuid();

    [Test]
    public async Task IsActiveStudentAsync_TrueForActiveStudentOnThatOffering()
    {
        var student = Guid.NewGuid();
        var reader = new InMemoryMembershipReader(new[]
        {
            new OfferingMembership(student, Offering, MembershipKind.Student, IsActive: true)
        });

        Assert.That(await reader.IsActiveStudentAsync(student, Offering), Is.True);
    }

    [Test]
    public async Task IsActiveStudentAsync_FalseForInactiveMembership()
    {
        var student = Guid.NewGuid();
        var reader = new InMemoryMembershipReader(new[]
        {
            new OfferingMembership(student, Offering, MembershipKind.Student, IsActive: false)
        });

        Assert.That(await reader.IsActiveStudentAsync(student, Offering), Is.False);
    }

    [Test]
    public async Task IsActiveStudentAsync_FalseForOtherOffering()
    {
        var student = Guid.NewGuid();
        var reader = new InMemoryMembershipReader(new[]
        {
            new OfferingMembership(student, Offering, MembershipKind.Student, IsActive: true)
        });

        Assert.That(await reader.IsActiveStudentAsync(student, OtherOffering), Is.False);
    }

    [TestCase(MembershipKind.Teacher)]
    [TestCase(MembershipKind.Assistant)]
    public async Task IsEngagedStaffAsync_TrueForTeacherOrAssistantOnThatOffering(MembershipKind kind)
    {
        var staff = Guid.NewGuid();
        var reader = new InMemoryMembershipReader(new[]
        {
            new OfferingMembership(staff, Offering, kind, IsActive: true)
        });

        var onOffering = await reader.IsEngagedStaffAsync(staff, Offering);
        var onOtherOffering = await reader.IsEngagedStaffAsync(staff, OtherOffering);

        Assert.Multiple(() =>
        {
            Assert.That(onOffering, Is.True);
            Assert.That(onOtherOffering, Is.False);
        });
    }

    [Test]
    public async Task IsEngagedStaffAsync_FalseForStudent()
    {
        var student = Guid.NewGuid();
        var reader = new InMemoryMembershipReader(new[]
        {
            new OfferingMembership(student, Offering, MembershipKind.Student, IsActive: true)
        });

        Assert.That(await reader.IsEngagedStaffAsync(student, Offering), Is.False);
    }

    [Test]
    public void UserContext_RejectsEmptyUser()
    {
        Assert.That(() => new UserContext(Guid.Empty, UserRole.Student), Throws.TypeOf<ArgumentException>());
    }
}
