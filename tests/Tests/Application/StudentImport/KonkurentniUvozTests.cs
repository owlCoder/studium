using Application.StudentImport;
using Infrastructure.StudentImport;
using NUnit.Framework;

namespace Tests.Application.StudentImport;

/// <summary>Проверава конкурентни увоз и атомарне операције портова.</summary>
[TestFixture]
public sealed class KonkurentniUvozTests
{
    /// <summary>Пропушта посао без трансакције да тест провери атомарне портове без серијализације.</summary>
    private sealed class ConcurrentPassThroughUnitOfWork : IUnitOfWork
    {
        public Task<T> ExecuteAsync<T>(
            Func<CancellationToken, Task<T>> work,
            CancellationToken cancellationToken = default)
            => work(cancellationToken);
    }

    /// <summary>Сви позиваоци стижу до покушаја регистрације пре првог уписа.</summary>
    private sealed class CoordinatedAccountDirectory(IAccountDirectory inner, int callers) : IAccountDirectory
    {
        private readonly object _gate = new();
        private readonly Dictionary<string, Arrival> _arrivals = new(UsernameComparison.Comparer);
        private int _attempts;
        public int Attempts => Volatile.Read(ref _attempts);
        private sealed class Arrival
        {
            public int Count;
            public TaskCompletionSource Ready { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }
        public Task<AccountSnapshot?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default)
            => inner.FindByUsernameAsync(username, cancellationToken);
        public async Task<AccountRegistrationResult> TryAddAsync(NewUserAccount account, CancellationToken cancellationToken = default)
        {
            Arrival arrival;
            lock (_gate)
            {
                if (!_arrivals.TryGetValue(account.Username, out arrival!))
                    _arrivals[account.Username] = arrival = new Arrival();
                arrival.Count++;
                if (arrival.Count == callers) arrival.Ready.TrySetResult();
            }
            Interlocked.Increment(ref _attempts);
            await arrival.Ready.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
            return await inner.TryAddAsync(account, cancellationToken);
        }
    }

    private sealed class YieldingEnrollmentDirectory : IEnrollmentDirectory
    {
        private readonly IEnrollmentDirectory _inner;

        public YieldingEnrollmentDirectory(IEnrollmentDirectory inner) => _inner = inner;

        public async Task<EnrollmentSnapshot?> FindActiveAsync(
            Guid accountId,
            Guid offeringId,
            CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return await _inner.FindActiveAsync(accountId, offeringId, cancellationToken);
        }

        public async Task<bool> TryAddActiveAsync(
            NewEnrollment enrollment,
            CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return await _inner.TryAddActiveAsync(enrollment, cancellationToken);
        }
    }

    [Test]
    public async Task Paralelne_Potvrde_Istog_Spiska_Nad_Deljenim_UnitOfWork_Ne_Dupliraju_Podatke()
    {
        var fixture = new StudentImportFixture();
        using var startGate = new ManualResetEventSlim(false);

        var tasks = Enumerable.Range(0, 8)
            .Select(_ => Task.Run(async () =>
            {
                startGate.Wait();
                var handler = fixture.ConfirmFor(fixture.AdminId, AccountRoles.Administrator);
                return await handler.HandleAsync(fixture.OfferingId, StudentImportFixture.SpecExample);
            }))
            .ToArray();

        startGate.Set();
        var results = await Task.WhenAll(tasks);

        Assert.That(results.Sum(r => r.CreatedAccountCount), Is.EqualTo(17));
        Assert.That(results.Sum(r => r.CreatedMembershipCount), Is.EqualTo(17));
        Assert.That(fixture.Enrollments.Count, Is.EqualTo(17));
        Assert.That(fixture.Activations.ActivationRequested, Has.Count.EqualTo(17));
        Assert.That(fixture.Activations.ActivationRequested.Distinct().Count(), Is.EqualTo(17));
    }

    [Test]
    public async Task Trka_Dva_Nezavisna_Uvoza_Na_Istom_Izvodjenju_Kreira_Jedan_Nalog_Jednu_Aktivaciju_I_Jedno_Clanstvo()
    {
        var fixture = new StudentImportFixture();
        var coordinatedAccounts = new CoordinatedAccountDirectory(fixture.Accounts, callers: 6);
        var yieldingEnrollments = new YieldingEnrollmentDirectory(fixture.Enrollments);
        var classifier = new ImportRowClassifier(coordinatedAccounts, yieldingEnrollments);
        var processor = new ImportRowProcessor(coordinatedAccounts, yieldingEnrollments, fixture.Activations);
        var passThroughUow = new ConcurrentPassThroughUnitOfWork();

        const string csv =
            "firstname,lastname,username,email\n" +
            "Ana,Anić,ana.anic,ana@example.com\n" +
            "Marko,Marković,marko.markovic,marko@example.com\n";

        ConfirmStudentImportHandler CreateHandler() => new(
            fixture.AuthorizerFor(fixture.AdminId, AccountRoles.Administrator),
            fixture.OfferingGuard,
            fixture.Parser,
            classifier,
            processor,
            passThroughUow,
            fixture.Clock);

        var results = await Task.WhenAll(
            Enumerable.Range(0, 6).Select(_ => CreateHandler().HandleAsync(fixture.OfferingId, csv)));

        Assert.That(results.Sum(r => r.CreatedAccountCount), Is.EqualTo(2));
        Assert.That(results.Sum(r => r.CreatedMembershipCount), Is.EqualTo(2));
        Assert.That(coordinatedAccounts.Attempts, Is.EqualTo(12));
        Assert.That(results.Sum(r => r.DuplicateCount), Is.EqualTo(10));
        Assert.That(results.Sum(r => r.ErrorCount), Is.Zero);
        Assert.That(fixture.Enrollments.Count, Is.EqualTo(2));
        Assert.That(fixture.Activations.ActivationRequested, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task Trka_Dva_Nastavnika_Na_Razlicitim_Izvodjenjima_Za_Istog_Novog_Studenta_Ne_Puca_I_Upisuje_Oba_Clanstva()
    {
        var fixture = new StudentImportFixture();
        var secondOfferingId = Guid.NewGuid();
        var secondTeacherId = Guid.NewGuid();

        fixture.Offerings.Seed(new OfferingSnapshot(
            secondOfferingId,
            fixture.CourseId,
            Guid.NewGuid(),
            OfferingState.Aktivno,
            IsArchived: false));
        fixture.Assignments.Seed(secondTeacherId, secondOfferingId, TeachingResponsibility.OdgovorniNastavnik);

        var coordinatedAccounts = new CoordinatedAccountDirectory(fixture.Accounts, callers: 2);
        var yieldingEnrollments = new YieldingEnrollmentDirectory(fixture.Enrollments);
        var classifier = new ImportRowClassifier(coordinatedAccounts, yieldingEnrollments);
        var processor = new ImportRowProcessor(coordinatedAccounts, yieldingEnrollments, fixture.Activations);
        var passThroughUow = new ConcurrentPassThroughUnitOfWork();

        const string csv =
            "firstname,lastname,username,email\n" +
            "Ana,Anić,ana.anic,ana@example.com\n";

        var firstHandler = new ConfirmStudentImportHandler(
            fixture.AuthorizerFor(fixture.TeacherId, AccountRoles.Nastavnik),
            fixture.OfferingGuard,
            fixture.Parser,
            classifier,
            processor,
            passThroughUow,
            fixture.Clock);

        var secondHandler = new ConfirmStudentImportHandler(
            fixture.AuthorizerFor(secondTeacherId, AccountRoles.Nastavnik),
            fixture.OfferingGuard,
            fixture.Parser,
            classifier,
            processor,
            passThroughUow,
            fixture.Clock);

        var results = await Task.WhenAll(
            firstHandler.HandleAsync(fixture.OfferingId, csv),
            secondHandler.HandleAsync(secondOfferingId, csv));

        // Један заједнички налог и једна прва активација, али по једно активно чланство на сваком извођењу.
        Assert.That(results.Sum(r => r.CreatedAccountCount), Is.EqualTo(1));
        Assert.That(results.Sum(r => r.CreatedMembershipCount), Is.EqualTo(2));
        Assert.That(fixture.Activations.ActivationRequested, Has.Count.EqualTo(1));
        Assert.That(fixture.Enrollments.Count, Is.EqualTo(2));
        Assert.That(coordinatedAccounts.Attempts, Is.EqualTo(2));
        var account = await fixture.Accounts.FindByUsernameAsync("ana.anic");
        Assert.That(fixture.Enrollments.All.Select(e => e.OfferingId),
            Is.EquivalentTo(new[] { fixture.OfferingId, secondOfferingId }));
        Assert.That(fixture.Enrollments.All.All(e => e.AccountId == account!.AccountId), Is.True);
    }

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

    [Test]
    public async Task Paralelni_Neuspesan_Uvoz_Vraca_Samo_Svoje_Izmene_I_Ne_Kvari_Uspesan_Uvoz()
    {
        var fixture = new StudentImportFixture();
        var failingActivations = new FailOnUsernameActivationService(
            fixture.Accounts, fixture.Activations, "padni.ovde");

        var failingProcessor = new ImportRowProcessor(
            fixture.Accounts, fixture.Enrollments, failingActivations);

        var failingHandler = new ConfirmStudentImportHandler(
            fixture.AuthorizerFor(fixture.AdminId, AccountRoles.Administrator),
            fixture.OfferingGuard,
            fixture.Parser,
            fixture.Classifier,
            failingProcessor,
            fixture.UnitOfWork,
            fixture.Clock);

        var validHandler = fixture.ConfirmFor(fixture.AdminId, AccountRoles.Administrator);

        const string badBatch =
            "firstname,lastname,username\n" +
            "Prvi,Privremeni,privremeni.jedan\n" +
            "Drugi,Pada,padni.ovde\n";

        const string goodBatch =
            "firstname,lastname,username\n" +
            "Ana,Anić,ana.anic\n" +
            "Marko,Marković,marko.markovic\n";

        var badTask = Task.Run(async () =>
        {
            try
            {
                await failingHandler.HandleAsync(fixture.OfferingId, badBatch);
                return false;
            }
            catch (InvalidOperationException)
            {
                return true;
            }
        });

        var goodTask = Task.Run(() => validHandler.HandleAsync(fixture.OfferingId, goodBatch));

        var failedAsExpected = await badTask;
        var goodResult = await goodTask;

        Assert.That(failedAsExpected, Is.True);
        Assert.That(goodResult.CreatedAccountCount, Is.EqualTo(2));
        Assert.That(goodResult.CreatedMembershipCount, Is.EqualTo(2));
        Assert.That(await fixture.Accounts.FindByUsernameAsync("privremeni.jedan"), Is.Null);
        Assert.That(await fixture.Accounts.FindByUsernameAsync("padni.ovde"), Is.Null);
        Assert.That(await fixture.Accounts.FindByUsernameAsync("ana.anic"), Is.Not.Null);
        Assert.That(await fixture.Accounts.FindByUsernameAsync("marko.markovic"), Is.Not.Null);
        Assert.That(fixture.Enrollments.Count, Is.EqualTo(2));
        Assert.That(fixture.Activations.ActivationRequested, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task Brojac_Novih_Naloga_Obuhvata_I_Ishod_Duplikatnog_Clanstva()
    {
        var fixture = new StudentImportFixture();
        var handler = new ConfirmStudentImportHandler(
            fixture.AuthorizerFor(fixture.AdminId, AccountRoles.Administrator),
            fixture.OfferingGuard,
            fixture.Parser,
            fixture.Classifier,
            new LostEnrollmentRaceProcessor(),
            fixture.UnitOfWork,
            fixture.Clock);

        const string csv =
            "firstname,lastname,username\n" +
            "Ana,Anić,ana.anic\n" +
            "Marko,Marković,marko.markovic\n";

        var result = await handler.HandleAsync(fixture.OfferingId, csv);

        // Замена процесора враћа нов налог и дупликатно чланство: бројачи су независни.
        Assert.That(result.CreatedAccountCount, Is.EqualTo(2));
        Assert.That(result.CreatedMembershipCount, Is.Zero);
        Assert.That(result.DuplicateCount, Is.EqualTo(2));
        Assert.That(result.ErrorCount, Is.Zero);
    }

    /// <summary>Замена за проверу бројача: враћа дупликатно чланство уз нов налог.</summary>
    private sealed class LostEnrollmentRaceProcessor : IImportRowProcessor
    {
        public Task<RowWriteResult> ProcessAsync(
            ParsedStudentRow row,
            Guid offeringId,
            DateTimeOffset enrolledAt,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new RowWriteResult(RowWriteOutcome.Duplicate, CreatedAccount: true));
    }

    private sealed class FailOnUsernameActivationService : IFirstActivationService
    {
        private readonly InMemoryStudentAccountDirectory _accounts;
        private readonly IFirstActivationService _inner;
        private readonly string _failUsername;

        public FailOnUsernameActivationService(
            InMemoryStudentAccountDirectory accounts,
            IFirstActivationService inner,
            string failUsername)
        {
            _accounts = accounts;
            _inner = inner;
            _failUsername = failUsername;
        }

        public async Task StartAsync(Guid accountId, CancellationToken cancellationToken = default)
        {
            var target = await _accounts.FindByUsernameAsync(_failUsername, cancellationToken);
            if (target is not null && target.AccountId == accountId)
                throw new InvalidOperationException("Simulirani pad aktivacije.");

            await _inner.StartAsync(accountId, cancellationToken);
        }
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
