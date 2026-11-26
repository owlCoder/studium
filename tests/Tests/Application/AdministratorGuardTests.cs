using Application.Users;
using Domain.Users;
using Infrastructure.Users;
using NUnit.Framework;

namespace Tests.Application;

public class AdministratorGuardTests
{
    private static (AdministratorGuard Guard, InMemoryUserRepository Repo) Create()
    {
        var repo = new InMemoryUserRepository(DemoUsers.Seed());
        return (new AdministratorGuard(repo), repo);
    }

    [Test]
    public void Administrator_IsAllowed()
    {
        var (guard, _) = Create();

        Assert.DoesNotThrowAsync(() => guard.EnsureAdministratorAsync(DemoUsers.AdminId));
    }

    [Test]
    public void Student_IsRejected()
    {
        var (guard, _) = Create();

        Assert.ThrowsAsync<ActorNotAuthorizedException>(
            () => guard.EnsureAdministratorAsync(DemoUsers.StudentId));
    }

    [Test]
    public async Task Teacher_IsRejected()
    {
        var (guard, repo) = Create();
        var teacher = new User(Guid.NewGuid(), "Nina", "Nastavnik", "nina", UserRole.Nastavnik);
        await repo.AddAsync(teacher);

        Assert.ThrowsAsync<ActorNotAuthorizedException>(
            () => guard.EnsureAdministratorAsync(teacher.Id));
    }

    [Test]
    public void UnknownActor_IsNotAuthenticated()
    {
        var (guard, _) = Create();

        Assert.ThrowsAsync<ActorNotAuthenticatedException>(
            () => guard.EnsureAdministratorAsync(Guid.NewGuid()));
    }
}
