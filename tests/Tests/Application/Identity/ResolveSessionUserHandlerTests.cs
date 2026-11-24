using Application.Identity;
using Moq;
using NUnit.Framework;

namespace Tests.Application.Identity;

[TestFixture]
public sealed class ResolveSessionUserHandlerTests
{
    private const string Token = "test-token";

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public async Task BezTokena_VracaNull_INeCitaEvidencije(string? token)
    {
        var sessions = new Mock<ISessionReader>(MockBehavior.Strict);
        var accounts = new Mock<IAccountStatusReader>(MockBehavior.Strict);

        var result = await new ResolveSessionUserHandler(sessions.Object, accounts.Object)
            .HandleAsync(token);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task NevazecaSesija_VracaNull_INeProveravaNalog()
    {
        var sessions = new Mock<ISessionReader>();
        sessions.Setup(x => x.FindUserIdAsync(Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);
        var accounts = new Mock<IAccountStatusReader>(MockBehavior.Strict);

        var result = await new ResolveSessionUserHandler(sessions.Object, accounts.Object)
            .HandleAsync(Token);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task SesijaSaPraznimId_VracaNull()
    {
        var sessions = new Mock<ISessionReader>();
        sessions.Setup(x => x.FindUserIdAsync(Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.Empty);
        var accounts = new Mock<IAccountStatusReader>(MockBehavior.Strict);

        var result = await new ResolveSessionUserHandler(sessions.Object, accounts.Object)
            .HandleAsync(Token);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task VazecaSesija_AktivanNalog_VracaIdIzSesije()
    {
        var userId = Guid.NewGuid();
        var sessions = new Mock<ISessionReader>();
        sessions.Setup(x => x.FindUserIdAsync(Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);
        var accounts = new Mock<IAccountStatusReader>();
        accounts.Setup(x => x.IsActiveAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await new ResolveSessionUserHandler(sessions.Object, accounts.Object)
            .HandleAsync(Token);

        Assert.That(result, Is.EqualTo(userId));
    }

    [Test]
    public async Task VazecaSesija_NeaktivanNalog_VracaNull()
    {
        var userId = Guid.NewGuid();
        var sessions = new Mock<ISessionReader>();
        sessions.Setup(x => x.FindUserIdAsync(Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);
        var accounts = new Mock<IAccountStatusReader>();
        accounts.Setup(x => x.IsActiveAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await new ResolveSessionUserHandler(sessions.Object, accounts.Object)
            .HandleAsync(Token);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task DeaktivacijaIzmedjuDvaZahteva_DrugiZahtevOdbijen()
    {
        var userId = Guid.NewGuid();
        var sessions = new Mock<ISessionReader>();
        sessions.Setup(x => x.FindUserIdAsync(Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);
        var accounts = new Mock<IAccountStatusReader>();
        accounts.SetupSequence(x => x.IsActiveAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        var handler = new ResolveSessionUserHandler(sessions.Object, accounts.Object);

        var first = await handler.HandleAsync(Token);
        var second = await handler.HandleAsync(Token);

        Assert.That(first, Is.EqualTo(userId));
        Assert.That(second, Is.Null);
        accounts.Verify(x => x.IsActiveAsync(userId, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
