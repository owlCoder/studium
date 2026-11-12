using Infrastructure.Identity;
using NUnit.Framework;

namespace Tests.Infrastructure.Identity;

[TestFixture]
public sealed class InMemoryAccountStatusReaderTests
{
    [Test]
    public async Task NepoznatNalog_NijeAktivan()
    {
        var reader = new InMemoryAccountStatusReader();

        Assert.That(await reader.IsActiveAsync(Guid.NewGuid()), Is.False);
    }

    [Test]
    public async Task AktiviranPaDeaktiviranNalog_ViseNijeAktivan()
    {
        var reader = new InMemoryAccountStatusReader();
        var userId = Guid.NewGuid();

        reader.Activate(userId);
        Assert.That(await reader.IsActiveAsync(userId), Is.True);

        reader.Deactivate(userId);
        Assert.That(await reader.IsActiveAsync(userId), Is.False);
    }

    [Test]
    public void Activate_OdbijaPrazanId()
    {
        var reader = new InMemoryAccountStatusReader();

        Assert.Throws<ArgumentException>(() => reader.Activate(Guid.Empty));
    }
}
