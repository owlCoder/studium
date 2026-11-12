using Moq;
using NUnit.Framework;
using Application.Sessions;
using Application.Time;
using Domain.Sessions;

namespace Tests.Application;

/// <summary>Testovi provere sesije po tokenu (ST-002).</summary>
[TestFixture]
public sealed class ResolveSessionHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid UserId = Guid.NewGuid();

    private static (ResolveSessionHandler Handler, Mock<ISessionStore> Store) Create(Session? stored)
    {
        var store = new Mock<ISessionStore>();
        store.Setup(x => x.FindByTokenHashAsync("hes(token)", It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);

        var tokens = new Mock<ISessionTokenProvider>();
        tokens.Setup(x => x.Hash(It.IsAny<string>())).Returns<string>(t => "hes(" + t + ")");

        var clock = new Mock<IClock>();
        clock.Setup(x => x.UtcNow).Returns(Now);

        return (new ResolveSessionHandler(store.Object, tokens.Object, clock.Object), store);
    }

    private static Session SessionExpiringAt(DateTimeOffset expiresAt) =>
        new("hes(token)", UserId, Now.AddHours(-8), expiresAt);

    [Test]
    public async Task HandleAsync_ReturnsUserId_ForValidSession()
    {
        var (handler, _) = Create(SessionExpiringAt(Now.AddMinutes(1)));

        Assert.That(await handler.HandleAsync("token"), Is.EqualTo(UserId));
    }

    [Test]
    public async Task HandleAsync_ReturnsNull_ForUnknownToken()
    {
        var (handler, _) = Create(stored: null);

        Assert.That(await handler.HandleAsync("token"), Is.Null);
    }

    [Test]
    public async Task HandleAsync_ReturnsNull_ForExpiredSession()
    {
        var (handler, _) = Create(SessionExpiringAt(Now.AddMinutes(-1)));

        Assert.That(await handler.HandleAsync("token"), Is.Null);
    }

    [Test]
    public async Task HandleAsync_ReturnsNull_AtTheExactMomentOfExpiry()
    {
        var (handler, _) = Create(SessionExpiringAt(Now));

        Assert.That(await handler.HandleAsync("token"), Is.Null);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public async Task HandleAsync_ReturnsNull_WithoutTouchingStore_WhenTokenIsBlank(string? token)
    {
        var (handler, store) = Create(SessionExpiringAt(Now.AddMinutes(1)));

        Assert.That(await handler.HandleAsync(token), Is.Null);
        store.Verify(
            x => x.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public void HandleAsync_DoesNotSwallowStoreFailure()
    {
        var store = new Mock<ISessionStore>();
        store.Setup(x => x.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Skladište nije dostupno."));
        var handler = new ResolveSessionHandler(
            store.Object, Mock.Of<ISessionTokenProvider>(), Mock.Of<IClock>());

        // Tehnička greška ne sme da izgleda kao "sesija ne važi".
        Assert.That(async () => await handler.HandleAsync("token"),
            Throws.TypeOf<InvalidOperationException>());
    }
}
