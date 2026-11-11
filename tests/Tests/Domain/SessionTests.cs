using NUnit.Framework;
using Domain.Sessions;

namespace Tests.Domain;

[TestFixture]
public sealed class SessionTests
{
    private static readonly DateTimeOffset Created = new(2026, 11, 10, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Expires = Created.AddHours(8);

    private static Session Valid() => new("hes", Guid.NewGuid(), Created, Expires);

    [Test]
    public void IsExpiredAt_IsFalse_BeforeExpiry()
    {
        Assert.That(Valid().IsExpiredAt(Expires.AddSeconds(-1)), Is.False);
    }

    [Test]
    public void IsExpiredAt_IsTrue_AtTheExactMomentOfExpiry()
    {
        Assert.That(Valid().IsExpiredAt(Expires), Is.True);
    }

    [Test]
    public void IsExpiredAt_IsTrue_AfterExpiry()
    {
        Assert.That(Valid().IsExpiredAt(Expires.AddSeconds(1)), Is.True);
    }

    [Test]
    public void Constructor_RejectsExpiryThatIsNotAfterCreation()
    {
        Assert.That(() => new Session("hes", Guid.NewGuid(), Created, Created),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Constructor_RejectsBlankTokenHash()
    {
        Assert.That(() => new Session(" ", Guid.NewGuid(), Created, Expires),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Constructor_RejectsEmptyUserId()
    {
        Assert.That(() => new Session("hes", Guid.Empty, Created, Expires),
            Throws.TypeOf<ArgumentException>());
    }
}
