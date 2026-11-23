using NUnit.Framework;
using Domain.Attendance;

namespace Tests.Domain.Attendance;

[TestFixture]
public sealed class MembershipPeriodTests
{
    private static readonly DateTime Start = new(2026, 10, 1, 0, 0, 0);
    private static readonly DateTime End = new(2026, 11, 10, 0, 0, 0);

    [Test]
    public void Create_EndAfterStart_KeepsBothBounds()
    {
        var period = new MembershipPeriod(Start, End);

        Assert.Multiple(() =>
        {
            Assert.That(period.From, Is.EqualTo(Start));
            Assert.That(period.To, Is.EqualTo(End));
        });
    }

    [Test]
    public void Create_EndBeforeStart_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => new MembershipPeriod(End, Start));
    }

    [Test]
    public void Create_EndEqualToStart_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => new MembershipPeriod(Start, Start));
    }

    [Test]
    public void Create_WithoutEnd_IsOpenEnded()
    {
        var period = new MembershipPeriod(Start);

        Assert.Multiple(() =>
        {
            Assert.That(period.To, Is.Null);
            Assert.That(period.Includes(new DateTime(2030, 1, 1)), Is.True);
        });
    }

    [Test]
    public void Includes_MomentInsidePeriod_IsTrue()
    {
        var period = new MembershipPeriod(Start, End);

        Assert.That(period.Includes(new DateTime(2026, 11, 7, 10, 0, 0)), Is.True);
    }

    [Test]
    public void Includes_MomentBeforeStart_IsFalse()
    {
        var period = new MembershipPeriod(Start, End);

        Assert.That(period.Includes(Start.AddMinutes(-1)), Is.False);
    }

    [Test]
    public void Includes_MomentAfterEnd_IsFalse()
    {
        var period = new MembershipPeriod(Start, End);

        Assert.That(period.Includes(End.AddDays(1)), Is.False);
    }

    [Test]
    public void Always_IncludesAnyMoment()
    {
        Assert.Multiple(() =>
        {
            Assert.That(MembershipPeriod.Always.Includes(new DateTime(2000, 1, 1)), Is.True);
            Assert.That(MembershipPeriod.Always.Includes(new DateTime(2099, 1, 1)), Is.True);
        });
    }
}
