using Infrastructure.Identity;
using NUnit.Framework;

namespace Tests.Infrastructure.Identity;

[TestFixture]
public sealed class InMemorySessionReaderTests
{
    [Test]
    public async Task RegistrovanToken_VracaKorisnika()
    {
        var reader = new InMemorySessionReader();
        var userId = Guid.NewGuid();
        reader.Register("token-a", userId);

        Assert.That(await reader.FindUserIdAsync("token-a"), Is.EqualTo(userId));
    }

    [Test]
    public async Task NepoznatToken_VracaNull()
    {
        var reader = new InMemorySessionReader();
        reader.Register("token-a", Guid.NewGuid());

        Assert.That(await reader.FindUserIdAsync("token-b"), Is.Null);
    }

    [Test]
    public async Task OpozvanaSesija_ViseNeVazi()
    {
        var reader = new InMemorySessionReader();
        reader.Register("token-a", Guid.NewGuid());

        Assert.That(reader.Revoke("token-a"), Is.True);
        Assert.That(await reader.FindUserIdAsync("token-a"), Is.Null);
    }

    [Test]
    public void Register_OdbijaPrazanTokenIPrazanId()
    {
        var reader = new InMemorySessionReader();

        Assert.Throws<ArgumentException>(() => reader.Register(" ", Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => reader.Register("token-a", Guid.Empty));
    }
}
