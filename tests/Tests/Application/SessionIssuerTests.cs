using Moq;
using NUnit.Framework;
using Application.Sessions;
using Application.Time;
using Domain.Sessions;

namespace Tests.Application;

/// <summary>Testovi izdavanja sesije (ST-002). Portovi su Moq objekti, vreme je lažni sat.</summary>
[TestFixture]
public sealed class SessionIssuerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(8);

    private static readonly Guid AnaId = Guid.NewGuid();

    private static (SessionIssuer Issuer, Mock<ISessionStore> Store, Mock<ISessionTokenProvider> Tokens)
        Create()
    {
        var store = new Mock<ISessionStore>();

        var tokens = new Mock<ISessionTokenProvider>();
        tokens.Setup(x => x.Generate()).Returns("sirovi-token");
        tokens.Setup(x => x.Hash(It.IsAny<string>())).Returns<string>(t => "hes(" + t + ")");

        var clock = new Mock<IClock>();
        clock.Setup(x => x.UtcNow).Returns(Now);

        var issuer = new SessionIssuer(
            store.Object, tokens.Object, clock.Object, new SessionSettings(Lifetime));

        return (issuer, store, tokens);
    }

    [Test]
    public async Task IssueAsync_ReturnsToken_AndExpiryCountedFromTheClock()
    {
        var (issuer, _, _) = Create();

        var issued = await issuer.IssueAsync(AnaId);

        Assert.Multiple(() =>
        {
            Assert.That(issued.Token, Is.EqualTo("sirovi-token"));
            Assert.That(issued.ExpiresAt, Is.EqualTo(Now + Lifetime));
        });
    }

    [Test]
    public async Task IssueAsync_StoresOnlyTheTokenHash_NeverTheToken()
    {
        var (issuer, store, _) = Create();
        Session? saved = null;
        store.Setup(x => x.AddAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()))
            .Callback<Session, CancellationToken>((s, _) => saved = s)
            .Returns(Task.CompletedTask);

        await issuer.IssueAsync(AnaId);

        Assert.Multiple(() =>
        {
            Assert.That(saved, Is.Not.Null);
            Assert.That(saved!.TokenHash, Is.EqualTo("hes(sirovi-token)"));
            Assert.That(saved.TokenHash, Is.Not.EqualTo("sirovi-token"));
            Assert.That(saved.UserId, Is.EqualTo(AnaId));
            Assert.That(saved.CreatedAt, Is.EqualTo(Now));
            Assert.That(saved.ExpiresAt, Is.EqualTo(Now + Lifetime));
        });
    }

    [Test]
    public async Task IssueAsync_GeneratesNewToken_ForEveryLogin()
    {
        var (issuer, _, tokens) = Create();

        await issuer.IssueAsync(AnaId);
        await issuer.IssueAsync(AnaId);

        tokens.Verify(x => x.Generate(), Times.Exactly(2));
    }

    [Test]
    public void Settings_RejectNonPositiveLifetime()
    {
        Assert.That(() => new SessionSettings(TimeSpan.Zero),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void IssueAsync_DoesNotSwallowStoreFailure()
    {
        var (issuer, store, _) = Create();
        store.Setup(x => x.AddAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Skladište nije dostupno."));

        // Ako sesija nije sačuvana, prijava ne sme da izgleda uspešno.
        Assert.That(async () => await issuer.IssueAsync(AnaId),
            Throws.TypeOf<InvalidOperationException>());
    }
}
