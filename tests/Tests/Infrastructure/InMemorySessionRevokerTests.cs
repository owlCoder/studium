using Application.Time;
using Infrastructure.Users;
using NSubstitute;
using NUnit.Framework;

namespace Tests.Infrastructure;

[TestFixture]
public sealed class InMemorySessionRevokerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private IClock _clock = null!;
    private InMemorySessionRevoker _revoker = null!;

    [SetUp]
    public void SetUp()
    {
        _clock = Substitute.For<IClock>();
        _clock.UtcNow.Returns(Now);
        _revoker = new InMemorySessionRevoker(_clock);
    }

    [Test]
    public void GetLastRevokedAt_IsNull_BeforeLogout()
    {
        Assert.That(_revoker.GetLastRevokedAt(Guid.NewGuid()), Is.Null);
    }

    [Test]
    public async Task RevokeAsync_RecordsCurrentTime()
    {
        var userId = Guid.NewGuid();

        await _revoker.RevokeAsync(userId);

        Assert.That(_revoker.GetLastRevokedAt(userId), Is.EqualTo(Now));
    }

    [Test]
    public async Task IsRevoked_IsTrue_ForSessionIssuedBeforeLogout()
    {
        var userId = Guid.NewGuid();
        await _revoker.RevokeAsync(userId);

        Assert.That(_revoker.IsRevoked(userId, Now.AddMinutes(-5)), Is.True);
    }

    [Test]
    public async Task IsRevoked_IsFalse_ForSessionIssuedAfterLogout()
    {
        var userId = Guid.NewGuid();
        await _revoker.RevokeAsync(userId);

        Assert.That(_revoker.IsRevoked(userId, Now.AddMinutes(5)), Is.False);
    }

    [Test]
    public async Task RevokeAsync_DoesNotAffectOtherUsers()
    {
        await _revoker.RevokeAsync(Guid.NewGuid());

        Assert.That(_revoker.IsRevoked(Guid.NewGuid(), Now.AddMinutes(-5)), Is.False);
    }
}
