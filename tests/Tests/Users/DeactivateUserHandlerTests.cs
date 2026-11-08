using Application.Users;
using Domain.Users;
using Infrastructure.Users;
using NUnit.Framework;

namespace Tests.Users;

[TestFixture]
public class DeactivateUserHandlerTests
{
    private static User NewUser() =>
        new(Guid.NewGuid(), "Pera", "Peric", "pera", UserRole.Student);

    [Test]
    public async Task Deactivate_ExistingUser_SetsInactive()
    {
        var user = NewUser();
        var repo = new InMemoryUserRepository(new[] { user });
        var handler = new DeactivateUserHandler(repo);

        await handler.HandleAsync(user.Id);

        var saved = await repo.GetByIdAsync(user.Id);
        Assert.That(saved!.Status, Is.EqualTo(UserStatus.Inactive));
    }

    [Test]
    public void Deactivate_UnknownUser_ThrowsArgumentException()
    {
        var handler = new DeactivateUserHandler(new InMemoryUserRepository());

        Assert.ThrowsAsync<ArgumentException>(() => handler.HandleAsync(Guid.NewGuid()));
    }

    [Test]
    public async Task Deactivate_AlreadyInactive_ThrowsInvalidOperationException()
    {
        var user = NewUser();
        var handler = new DeactivateUserHandler(new InMemoryUserRepository(new[] { user }));
        await handler.HandleAsync(user.Id);

        Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(user.Id));
    }
}
