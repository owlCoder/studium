using NUnit.Framework;
using Infrastructure.Sessions;

namespace Tests.Infrastructure;

[TestFixture]
public sealed class SecureSessionTokenProviderTests
{
    private readonly SecureSessionTokenProvider _provider = new();

    [Test]
    public void Generate_ReturnsDifferentToken_EveryTime()
    {
        var tokens = Enumerable.Range(0, 100).Select(_ => _provider.Generate()).ToList();

        Assert.That(tokens.Distinct().Count(), Is.EqualTo(100));
    }

    [Test]
    public void Generate_ReturnsLongTokenSafeForHeadersAndUrls()
    {
        var token = _provider.Generate();

        Assert.Multiple(() =>
        {
            // 32 bajta = 256 bita, zapisano u 43 znaka bez dopune.
            Assert.That(token, Has.Length.EqualTo(43));
            Assert.That(token, Does.Match("^[A-Za-z0-9_-]+$"));
        });
    }

    [Test]
    public void Hash_IsDeterministic_AndDiffersFromToken()
    {
        var token = _provider.Generate();

        Assert.Multiple(() =>
        {
            Assert.That(_provider.Hash(token), Is.EqualTo(_provider.Hash(token)));
            Assert.That(_provider.Hash(token), Is.Not.EqualTo(token));
            Assert.That(_provider.Hash(token), Does.Not.Contain(token));
        });
    }

    [Test]
    public void Hash_DiffersForDifferentTokens()
    {
        Assert.That(_provider.Hash("a"), Is.Not.EqualTo(_provider.Hash("b")));
    }
}
