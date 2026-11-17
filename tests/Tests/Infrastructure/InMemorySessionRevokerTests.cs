using Application.Time;
using Infrastructure.Users;
using NSubstitute;
using NUnit.Framework;

namespace Tests.Infrastructure;

[TestFixture]
public sealed class InMemorySessionRevokerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid UserId = Guid.NewGuid();

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
    public void IsRevoked_IsFalse_WhenNothingWasRevoked()
    {
        Assert.That(_revoker.IsRevoked("hash-a", UserId, Now.AddMinutes(-5)), Is.False);
    }

    [Test]
    public async Task RevokeSession_RevokesOnlyThatToken()
    {
        await _revoker.RevokeSessionAsync("hash-a");

        Assert.That(_revoker.IsRevoked("hash-a", UserId, Now.AddMinutes(-5)), Is.True);
        Assert.That(_revoker.IsRevoked("hash-b", UserId, Now.AddMinutes(-5)), Is.False);
    }

    [Test]
    public async Task RevokeOtherSessions_RevokesOlderSessions_ButKeepsTheCurrentOne()
    {
        await _revoker.RevokeOtherSessionsAsync(UserId, "hash-current");

        Assert.That(_revoker.IsRevoked("hash-other", UserId, Now.AddMinutes(-5)), Is.True);
        Assert.That(_revoker.IsRevoked("hash-current", UserId, Now.AddMinutes(-5)), Is.False);
    }

    [Test]
    public async Task RevokeOtherSessions_DoesNotAffectSessionsCreatedAfterwards()
    {
        await _revoker.RevokeOtherSessionsAsync(UserId, "hash-current");

        Assert.That(_revoker.IsRevoked("hash-new", UserId, Now.AddMinutes(5)), Is.False);
    }

    [Test]
    public async Task RevokeOtherSessions_DoesNotAffectOtherUsers()
    {
        await _revoker.RevokeOtherSessionsAsync(UserId, "hash-current");

        Assert.That(_revoker.IsRevoked("hash-x", Guid.NewGuid(), Now.AddMinutes(-5)), Is.False);
    }

    [Test]
    public async Task RevokeSession_DoesNotRevokeTheKeptSessionOfAnotherUser()
    {
        await _revoker.RevokeSessionAsync("hash-a");

        Assert.That(_revoker.IsRevoked("hash-b", Guid.NewGuid(), Now.AddMinutes(-5)), Is.False);
    }

    [Test]
    public void RevokeSession_Throws_WhenTokenHashIsMissing()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _revoker.RevokeSessionAsync(""));
    }

    [Test]
    public void RevokeOtherSessions_Throws_WhenArgumentsAreMissing()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _revoker.RevokeOtherSessionsAsync(Guid.Empty, "hash-a"));
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _revoker.RevokeOtherSessionsAsync(UserId, ""));
    }
}
