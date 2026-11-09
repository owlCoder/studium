using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Users;
using Domain.Users;
using Infrastructure.Users;
using NUnit.Framework;

namespace Tests.Users;

[TestFixture]
public class DeactivateUserHandlerTests
{
    private static User NewStudent() =>
        new(
            Guid.NewGuid(),
            "Pera",
            "Peric",
            "pera",
            UserRole.Student);

    private static User NewAdministrator(string username) =>
        new(
            Guid.NewGuid(),
            "Admin",
            "Test",
            username,
            UserRole.Administrator);

    [Test]
    public async Task Deactivate_ExistingStudent_ReturnsDeactivated()
    {
        var user = NewStudent();
        var repo = new InMemoryUserRepository(new[] { user });
        var handler = new DeactivateUserHandler(repo);

        var result = await handler.HandleAsync(user.Id);

        Assert.That(result, Is.EqualTo(DeactivateUserResult.Deactivated));

        var saved = await repo.GetByIdAsync(user.Id);
        Assert.That(saved!.Status, Is.EqualTo(UserStatus.Deactivated));
    }

    [Test]
    public async Task Deactivate_UnknownUser_ReturnsNotFound()
    {
        var handler = new DeactivateUserHandler(
            new InMemoryUserRepository());

        var result = await handler.HandleAsync(Guid.NewGuid());

        Assert.That(result, Is.EqualTo(DeactivateUserResult.NotFound));
    }

    [Test]
    public async Task Deactivate_AlreadyDeactivatedUser_ReturnsAlreadyDeactivated()
    {
        var user = NewStudent();
        var handler = new DeactivateUserHandler(
            new InMemoryUserRepository(new[] { user }));

        var firstResult = await handler.HandleAsync(user.Id);
        var secondResult = await handler.HandleAsync(user.Id);

        Assert.That(firstResult, Is.EqualTo(DeactivateUserResult.Deactivated));
        Assert.That(secondResult, Is.EqualTo(DeactivateUserResult.AlreadyDeactivated));
    }

    [Test]
    public async Task Deactivate_LastActiveAdministrator_IsRejected()
    {
        var administrator = NewAdministrator("admin");
        var repo = new InMemoryUserRepository(new[] { administrator });
        var handler = new DeactivateUserHandler(repo);

        var result = await handler.HandleAsync(administrator.Id);

        Assert.That(
            result,
            Is.EqualTo(DeactivateUserResult.LastActiveAdministrator));

        var saved = await repo.GetByIdAsync(administrator.Id);
        Assert.That(saved!.Status, Is.EqualTo(UserStatus.Active));
    }

    [Test]
    public async Task Deactivate_OneOfTwoAdministrators_IsAllowed()
    {
        var firstAdmin = NewAdministrator("admin1");
        var secondAdmin = NewAdministrator("admin2");

        var repo = new InMemoryUserRepository(
            new[] { firstAdmin, secondAdmin });

        var handler = new DeactivateUserHandler(repo);

        var result = await handler.HandleAsync(firstAdmin.Id);

        Assert.That(result, Is.EqualTo(DeactivateUserResult.Deactivated));

        var remainingAdmin = await repo.GetByIdAsync(secondAdmin.Id);
        Assert.That(
            remainingAdmin!.Status,
            Is.EqualTo(UserStatus.Active));
    }

    [Test]
    public async Task GetById_ReturnsDetachedCopy()
    {
        var user = NewStudent();
        var repo = new InMemoryUserRepository(new[] { user });

        var retrieved = await repo.GetByIdAsync(user.Id);
        retrieved!.Deactivate();

        var saved = await repo.GetByIdAsync(user.Id);

        Assert.That(saved!.Status, Is.EqualTo(UserStatus.Active));
    }

    [Test]
    public async Task AddAsync_StoresDetachedCopy()
    {
        var user = NewStudent();
        var repo = new InMemoryUserRepository();

        await repo.AddAsync(user);

        // Menjamo original koji smo prosledili repozitorijumu.
        user.Deactivate();

        var saved = await repo.GetByIdAsync(user.Id);

        Assert.That(saved!.Status, Is.EqualTo(UserStatus.Active));
    }

    [Test]
    public async Task UpdateAsync_CannotChangeAccountStatusDirectly()
    {
        var user = NewStudent();
        var repo = new InMemoryUserRepository(new[] { user });

        var retrieved = await repo.GetByIdAsync(user.Id);
        retrieved!.Deactivate();

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await repo.UpdateAsync(retrieved));

        var saved = await repo.GetByIdAsync(user.Id);

        Assert.That(saved!.Status, Is.EqualTo(UserStatus.Active));
    }

    [Test]
    public async Task Deactivate_TwoAdministratorsConcurrently_OnlyOneSucceeds()
    {
        var firstAdmin = NewAdministrator("admin1");
        var secondAdmin = NewAdministrator("admin2");

        var repo = new InMemoryUserRepository(
            new[] { firstAdmin, secondAdmin });

        var handler = new DeactivateUserHandler(repo);

        using var ready = new CountdownEvent(2);
        using var start = new ManualResetEventSlim(false);

        Task<DeactivateUserResult> StartDeactivation(Guid userId)
        {
            return Task.Factory.StartNew(
                () =>
                {
                    ready.Signal();
                    start.Wait();
                    return handler.HandleAsync(userId);
                },
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default).Unwrap();
        }

        var firstTask = StartDeactivation(firstAdmin.Id);
        var secondTask = StartDeactivation(secondAdmin.Id);

        // Čekamo da oba threada budu spremna.
        ready.Wait();
        start.Set();

        var results = await Task.WhenAll(firstTask, secondTask);

        Assert.That(
            results.Count(r => r == DeactivateUserResult.Deactivated),
            Is.EqualTo(1));

        Assert.That(
            results.Count(r => r == DeactivateUserResult.LastActiveAdministrator),
            Is.EqualTo(1));

        var savedFirst = await repo.GetByIdAsync(firstAdmin.Id);
        var savedSecond = await repo.GetByIdAsync(secondAdmin.Id);

        int remainingAdministrators = new[] { savedFirst, savedSecond }
            .Count(u => u!.Role == UserRole.Administrator && u.IsActive);

        Assert.That(remainingAdministrators, Is.EqualTo(1));
    }
}
