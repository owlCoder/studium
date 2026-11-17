using Application.StudentImport;
using Infrastructure.StudentImport;
using NUnit.Framework;

namespace Tests.Application.StudentImport;

/// <summary>Проверава угнежђене трансакције и поништавање при неуспеху.</summary>
/// <remarks>Угнежђен позив не сме поново чекати закључавање спољашњег позива.</remarks>
[TestFixture]
[Timeout(10_000)]
public sealed class UgnjezdenaJedinicaPoslaTests
{
    /// <summary>Учесник за проверу броја отворених трансакција.</summary>
    private sealed class CountingParticipant : InMemoryTransaction
    {
        public int Begins { get; private set; }

        public int Commits { get; private set; }

        public int Rollbacks { get; private set; }

        public void Begin() => Begins++;

        public void Commit() => Commits++;

        public void Rollback() => Rollbacks++;
    }

    /// <summary>Угнежђен позив придружи се спољашњој транзакцији — нова се не отвара.</summary>
    [Test]
    public async Task Ugnjezdeni_Poziv_Ne_Otvara_Novu_Transakciju()
    {
        var participant = new CountingParticipant();
        var unitOfWork = new InMemoryUnitOfWork(participant);
        var result = await unitOfWork.ExecuteAsync(async _ =>
            await unitOfWork.ExecuteAsync(async __ => { await Task.Yield(); return "unutra"; }));
        Assert.That(result, Is.EqualTo("unutra"));
        Assert.That(participant.Begins, Is.EqualTo(1));
        Assert.That(participant.Commits, Is.EqualTo(1));
        Assert.That(participant.Rollbacks, Is.Zero);
    }

    /// <summary>Неухваћен изузетак из угнежђеног позива стиже до спољашњег и поништава цео посао.</summary>
    [Test]
    public void Ugnjezdeni_Poziv_Koji_Padne_Ponistava_Ceo_Posao()
    {
        var participant = new CountingParticipant();
        var unitOfWork = new InMemoryUnitOfWork(participant);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await unitOfWork.ExecuteAsync(async _ =>
                await unitOfWork.ExecuteAsync<string>(async __ =>
                {
                    await Task.Yield();
                    throw new InvalidOperationException("Unutrasnji posao je pao.");
                })));

        Assert.That(participant.Rollbacks, Is.EqualTo(1));
        Assert.That(participant.Commits, Is.Zero);
    }

    /// <summary>Закључавање се ослобађа и после неуспеха посла.</summary>
    [Test]
    public async Task Nakon_Posa_Zakljucavanje_Se_Oslobadja()
    {
        var unitOfWork = new InMemoryUnitOfWork(new CountingParticipant());

        await unitOfWork.ExecuteAsync(async _ => { await Task.Yield(); return 0; });
        var second = await unitOfWork.ExecuteAsync(async _ => { await Task.Yield(); return 7; });

        Assert.That(second, Is.EqualTo(7));
    }

    /// <summary>Паралелни позиви не деле контекст трансакције.</summary>
    [Test]
    public async Task Paralelni_Poslovi_Ne_Dele_Transakciju()
    {
        var participant = new CountingParticipant();
        var unit = new InMemoryUnitOfWork(participant);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = unit.ExecuteAsync(async ct => {
            entered.TrySetResult();
            await release.Task.WaitAsync(ct);
            return await unit.ExecuteAsync(_ => Task.FromResult(1), ct);
        });
        Task<int>? second = null;
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            second = unit.ExecuteAsync(ct => Task.FromResult(2));
            Assert.That(second.IsCompleted, Is.False);
        }
        finally
        {
            release.TrySetResult();
            await first.WaitAsync(TimeSpan.FromSeconds(5));
            if (second is not null) await second.WaitAsync(TimeSpan.FromSeconds(5));
        }
        Assert.That(await first, Is.EqualTo(1));
        Assert.That(await second!, Is.EqualTo(2));
        Assert.That(participant.Begins, Is.EqualTo(2));
        Assert.That(participant.Commits, Is.EqualTo(2));
        Assert.That(participant.Rollbacks, Is.Zero);
    }
}
