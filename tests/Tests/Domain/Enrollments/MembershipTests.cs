using Domain.Enrollments;
using Domain.Offerings;
using NUnit.Framework;

namespace Tests.Domain.Enrollments;

[TestFixture]
public sealed class MembershipTests

{
    [Test]
    public void NovoClanstvo_JeAktivno()
    {
        var membership = new Membership(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        Assert.That(membership.IsActive, Is.True);
    }

    [Test]
    public void Terminate_MenjaStatusUPrekinuto()
    {
        var membership = new Membership(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        membership.Terminate();

        Assert.Multiple(() =>
        {
            Assert.That(membership.Status, Is.EqualTo(MembershipStatus.Terminated));
            Assert.That(membership.IsActive, Is.False);
        });
    }

    [Test]
    public void Constructor_OdbijaPraznogStudenta()
    {
        Assert.That(
            () => new Membership(Guid.NewGuid(), Guid.Empty, Guid.NewGuid()),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Offering_OdbijaPraznuAkademskuGodinu()
    {
        Assert.That(
            () => new Offering(Guid.NewGuid(), Guid.NewGuid(), " "),
            Throws.TypeOf<ArgumentException>());
    }
}

