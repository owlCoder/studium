using Application.StudentImport;
using Infrastructure.StudentImport;
using NUnit.Framework;

namespace Tests.Application.StudentImport;

/// <summary>Поништавање трансакције враћа само њене измене.</summary>
/// <remarks>Поништавање једног посла не сме обрисати уписе другог посла.</remarks>
[TestFixture]
[Timeout(10_000)]
public sealed class IzolacijaTransakcijeTests
{
    [Test]
    public async Task Vracanje_Posla_Ne_Brise_Upis_Drugog_Posla()
    {
        var accounts = new InMemoryStudentAccountDirectory();
        var first = new InMemoryUnitOfWork(accounts);
        var second = new InMemoryUnitOfWork(accounts);
        var written = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstTask = first.ExecuteAsync<int>(async ct => {
            accounts.Seed("prvi.posao", Guid.NewGuid());
            written.TrySetResult();
            await release.Task.WaitAsync(ct);
            throw new InvalidOperationException("Prvi posao je pao.");
        });
        Task<int>? secondTask = null;
        Exception? failure = null;
        try
        {
            await written.Task.WaitAsync(TimeSpan.FromSeconds(5));
            secondTask = second.ExecuteAsync(ct => {
                accounts.Seed("drugi.posao", Guid.NewGuid());
                return Task.FromResult(1);
            });
            Assert.That(secondTask.IsCompleted, Is.False);
        }
        finally
        {
            release.TrySetResult();
            try { await firstTask.WaitAsync(TimeSpan.FromSeconds(5)); }
            catch (InvalidOperationException error) { failure = error; }
            if (secondTask is not null) await secondTask.WaitAsync(TimeSpan.FromSeconds(5));
        }
        Assert.That(failure, Is.TypeOf<InvalidOperationException>());
        Assert.That(await accounts.FindByUsernameAsync("prvi.posao"), Is.Null);
        Assert.That(await accounts.FindByUsernameAsync("drugi.posao"), Is.Not.Null);
    }

    [Test]
    public async Task Ugnezdena_Jedinica_Deli_Povratak_Posla()
    {
        var accounts = new InMemoryStudentAccountDirectory();
        var first = new InMemoryUnitOfWork(accounts);
        var second = new InMemoryUnitOfWork(accounts);
        Assert.ThrowsAsync<InvalidOperationException>(async () => await first.ExecuteAsync<int>(async ct => {
            accounts.Seed("prvi.posao", Guid.NewGuid());
            await second.ExecuteAsync(_ => {
                accounts.Seed("unutrasnji.posao", Guid.NewGuid());
                return Task.FromResult(1);
            }, ct);
            throw new InvalidOperationException("Prvi posao je pao.");
        }));
        Assert.That(await accounts.FindByUsernameAsync("prvi.posao"), Is.Null);
        Assert.That(await accounts.FindByUsernameAsync("unutrasnji.posao"), Is.Null);
    }

    [Test]
    public async Task Upis_Izvan_Posla_Nije_Ni_Pod_Uticajem_Vracanja()
    {
        var accounts = new InMemoryStudentAccountDirectory();
        var unitOfWork = new InMemoryUnitOfWork(accounts);

        accounts.Seed("pre.posla", Guid.NewGuid());

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await unitOfWork.ExecuteAsync<string>(_ =>
            {
                accounts.Seed("u.poslu", Guid.NewGuid());
                throw new InvalidOperationException("Posao je pao.");
            }));

        Assert.That(await accounts.FindByUsernameAsync("pre.posla"), Is.Not.Null);
        Assert.That(await accounts.FindByUsernameAsync("u.poslu"), Is.Null);
    }
}
