using NUnit.Framework;
using Domain.Sessions;
using Infrastructure.Sessions;

namespace Tests.Infrastructure;

[TestFixture]
public sealed class InMemorySessionStoreTests
{
    private static readonly DateTimeOffset Created = new(2026, 11, 10, 12, 0, 0, TimeSpan.Zero);

    private static Session SessionWith(string hash) =>
        new(hash, Guid.NewGuid(), Created, Created.AddHours(8));

    [Test]
    public async Task FindByTokenHashAsync_ReturnsStoredSession()
    {
        var store = new InMemorySessionStore();
        var session = SessionWith("hes-1");
        await store.AddAsync(session);

        Assert.That(await store.FindByTokenHashAsync("hes-1"), Is.SameAs(session));
    }

    [Test]
    public async Task FindByTokenHashAsync_ReturnsNull_WhenMissing()
    {
        var store = new InMemorySessionStore();

        Assert.That(await store.FindByTokenHashAsync("nema"), Is.Null);
    }

    [Test]
    public async Task AddAsync_RejectsSecondSession_WithTheSameTokenHash()
    {
        var store = new InMemorySessionStore();
        await store.AddAsync(SessionWith("hes-1"));

        Assert.That(async () => await store.AddAsync(SessionWith("hes-1")),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public async Task AddAsync_KeepsEverySession_WhenCalledConcurrently()
    {
        var store = new InMemorySessionStore();

        await Task.WhenAll(Enumerable.Range(0, 200)
            .Select(i => Task.Run(() => store.AddAsync(SessionWith("hes-" + i)))));

        for (int i = 0; i < 200; i++)
        {
            Assert.That(await store.FindByTokenHashAsync("hes-" + i), Is.Not.Null);
        }
    }
}
