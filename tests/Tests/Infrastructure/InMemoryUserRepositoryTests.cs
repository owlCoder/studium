using Domain.Users;
using Infrastructure.Users;
using NUnit.Framework;

namespace Tests.Infrastructure;

public class InMemoryUserRepositoryTests
{
    private static User NewUser(string username) =>
        new(Guid.NewGuid(), "Ime", "Prezime", username, UserRole.Student);

    [Test]
    public async Task AddAsync_RejectsDuplicateUsername_Sequential()
    {
        var repo = new InMemoryUserRepository();
        await repo.AddAsync(NewUser("marko"));

        Assert.ThrowsAsync<InvalidOperationException>(
            () => repo.AddAsync(NewUser("  MARKO ")));
    }

    [Test]
    public async Task AddAsync_AllowsOnlyOneUser_WhenAddedInParallel()
    {
        var repo = new InMemoryUserRepository();

        var tasks = Enumerable.Range(0, 50).Select(_ => Task.Run(async () =>
        {
            try
            {
                await repo.AddAsync(NewUser("Marko"));
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }));

        var results = await Task.WhenAll(tasks);

        Assert.That(results.Count(r => r), Is.EqualTo(1));
    }

    [Test]
    public async Task GetByUsernameAsync_FindsUser_IgnoringCaseAndSpaces()
    {
        var repo = new InMemoryUserRepository();
        var user = NewUser("marko");
        await repo.AddAsync(user);

        var found = await repo.GetByUsernameAsync("  MARKO ");

        Assert.That(found, Is.EqualTo(user));
    }

    [Test]
    public async Task UpdateAsync_RejectsUsernameTakenByAnotherUser()
    {
        var repo = new InMemoryUserRepository();
        var first = NewUser("marko");
        var second = NewUser("petar");
        await repo.AddAsync(first);
        await repo.AddAsync(second);

        var conflicting = new User(second.Id, "Ime", "Prezime", "marko", UserRole.Student);

        Assert.ThrowsAsync<InvalidOperationException>(() => repo.UpdateAsync(conflicting));
    }
}
