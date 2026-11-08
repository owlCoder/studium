using Domain.Users;
using Infrastructure.Users;
using NUnit.Framework;

namespace Tests.Infrastructure;

[TestFixture]
public sealed class InMemoryUserStoreTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private InMemoryUserStore _store = null!;

    [SetUp]
    public void SetUp()
    {
        _store = new InMemoryUserStore(new[]
        {
            new User(UserId, "ana", UserRole.Student, "old-hash", isActive: true)
        });
    }

    [Test]
    public async Task UpdatePasswordHash_IsVisibleToLoginLookup()
    {
        await _store.UpdatePasswordHashAsync(UserId, "new-hash");

        var user = await _store.FindByUsernameAsync("ana");

        Assert.That(user!.PasswordHash, Is.EqualTo("new-hash"));
        Assert.That(user.Role, Is.EqualTo(UserRole.Student));
        Assert.That(user.IsActive, Is.True);
    }

    [Test]
    public async Task GetPasswordHash_ReturnsNull_ForUnknownUser()
    {
        Assert.That(await _store.GetPasswordHashAsync(Guid.NewGuid()), Is.Null);
    }

    [Test]
    public void UpdatePasswordHash_Throws_ForUnknownUser()
    {
        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _store.UpdatePasswordHashAsync(Guid.NewGuid(), "x"));
    }

    [Test]
    public void Constructor_Throws_ForDuplicateUsernames()
    {
        Assert.Throws<ArgumentException>(() => new InMemoryUserStore(new[]
        {
            new User(Guid.NewGuid(), "Ana", UserRole.Student, "h", true),
            new User(Guid.NewGuid(), "ana", UserRole.Student, "h", true)
        }));
    }
}
