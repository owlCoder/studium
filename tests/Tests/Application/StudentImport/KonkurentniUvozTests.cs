using Application.StudentImport;
using Infrastructure.StudentImport;
using NUnit.Framework;

namespace Tests.Application.StudentImport;

/// <summary>Проверава конкурентни увоз и атомарне операције портова.</summary>
[TestFixture]
public sealed class KonkurentniUvozTests
{
    [Test]
    public async Task Ako_Drugi_Proces_U_Medjuvremenu_Kreira_Nestudentski_Nalog_Procesor_Vraca_Gresku_Bez_Upisa()
    {
        var fixture = new StudentImportFixture();
        var interceptor = new RaceSeedNonStudentAccountDirectory(fixture.Accounts);
        var processor = new ImportRowProcessor(interceptor, fixture.Enrollments, fixture.Activations);

        var outcome = await processor.ProcessAsync(
            new ParsedStudentRow(2, "nastavnik.novi", "Petar", "Petrović", string.Empty),
            fixture.OfferingId,
            StudentImportFixture.Now);

        Assert.That(outcome.Outcome, Is.EqualTo(RowWriteOutcome.Error));
        Assert.That(outcome.CreatedAccount, Is.False);
        Assert.That(fixture.Enrollments.Count, Is.Zero);
        Assert.That(fixture.Activations.ActivationRequested, Is.Empty);
    }

    private sealed class RaceSeedNonStudentAccountDirectory : IAccountDirectory
    {
        private readonly InMemoryStudentAccountDirectory _inner;

        public RaceSeedNonStudentAccountDirectory(InMemoryStudentAccountDirectory inner)
            => _inner = inner;

        public Task<AccountSnapshot?> FindByUsernameAsync(
            string username,
            CancellationToken cancellationToken = default)
            => Task.FromResult<AccountSnapshot?>(null);

        public Task<AccountRegistrationResult> TryAddAsync(
            NewUserAccount account,
            CancellationToken cancellationToken = default)
        {
            _inner.Seed(
                account.Username,
                Guid.NewGuid(),
                role: AccountRole.Nastavnik,
                state: AccountState.Aktivan);
            return _inner.TryAddAsync(account, cancellationToken);
        }
    }
}
