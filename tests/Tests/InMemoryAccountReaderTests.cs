using NUnit.Framework;
using Domain.Users;
using Infrastructure.Auth;

namespace Tests.Infrastructure;

[TestFixture]
public sealed class InMemoryAccountReaderTests
{
    private static User UserOf(UserRole role, string? passwordHash, bool isActive) =>
        new(Guid.NewGuid(), "korisnik", role, passwordHash, isActive);

    [Test]
    public async Task FindByIdAsync_ReturnsAccountState_ForExistingUser()
    {
        var user = UserOf(UserRole.Administrator, "heš", isActive: true);
        var reader = new InMemoryAccountReader(new[] { user });

        var account = await reader.FindByIdAsync(user.Id);

        Assert.That(account, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(account!.Id, Is.EqualTo(user.Id));
            Assert.That(account.Role, Is.EqualTo(UserRole.Administrator));
            Assert.That(account.IsActive, Is.True);
            Assert.That(account.IsActivated, Is.True);
        });
    }

    [Test]
    public async Task FindByIdAsync_ReturnsNull_ForUnknownId()
    {
        var reader = new InMemoryAccountReader(Array.Empty<User>());

        Assert.That(await reader.FindByIdAsync(Guid.NewGuid()), Is.Null);
    }

    [Test]
    public async Task FindByIdAsync_ShowsDeactivatedAccount_AsNotActive()
    {
        var user = UserOf(UserRole.Teacher, "heš", isActive: false);
        var reader = new InMemoryAccountReader(new[] { user });

        var account = await reader.FindByIdAsync(user.Id);

        Assert.That(account!.IsActive, Is.False);
    }

    [Test]
    public async Task FindByIdAsync_ShowsAccountWithoutPassword_AsNotActivated()
    {
        var user = UserOf(UserRole.Assistant, passwordHash: null, isActive: true);
        var reader = new InMemoryAccountReader(new[] { user });

        var account = await reader.FindByIdAsync(user.Id);

        Assert.That(account!.IsActivated, Is.False);
    }
}
