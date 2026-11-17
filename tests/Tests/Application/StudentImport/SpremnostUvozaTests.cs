using Application.StudentImport;
using Infrastructure.StudentImport;
using NUnit.Framework;

namespace Tests.Application.StudentImport;

[TestFixture]
public sealed class SpremnostUvozaTests
{
    [TestCase("ana.\"anic\"")]
    [TestCase("\"ana\"junk")]
    [TestCase("\"ana\" \"anic\"")]
    public async Task Pogresni_Navodnici_Ne_Menjaju_Identitet(string username)
    {
        var f = new StudentImportFixture();
        f.Accounts.Seed("ana.anic", Guid.NewGuid());
        var csv = $"firstname,lastname,username\nAna,Anic,{username}\n";
        Assert.ThrowsAsync<ImportFileRejectedException>(async () =>
            await f.ConfirmFor(f.AdminId, AccountRoles.Administrator).HandleAsync(f.OfferingId, csv));
        Assert.That(f.Enrollments.Count, Is.Zero);
        Assert.That(f.Activations.ActivationRequested, Is.Empty);
        Assert.That(await f.Accounts.FindByUsernameAsync("ana.anic"), Is.Not.Null);
    }

    [Test]
    public void Previse_Kolona_Se_Odbija_I_Kad_Su_Prazne()
    {
        Assert.Throws<ImportFileRejectedException>(() => new CsvReader().Read(new string(',', CsvReader.MaxColumns)));
        var cells = Enumerable.Range(0, CsvReader.MaxColumns).Select(i => "c" + i).ToArray();
        Assert.That(new CsvReader().Read(string.Join(',', cells)).Header.Cells.Count, Is.EqualTo(CsvReader.MaxColumns));
    }

    [Test]
    public void Pravila_Ne_Mogu_Tiho_Promeniti_Izvor_Naloga()
    {
        var a = new InMemoryStudentAccountDirectory();
        var b = new InMemoryStudentAccountDirectory();
        Assert.Throws<ArgumentException>(() => new ImportRowClassifier(
            Array.Empty<IImportRowRule>(), new RepeatedUsernameRule(),
            new IImportRowRule[] { new StudentNameRequiredRule(a), new AccountEligibilityRule(b) }));
    }

    [TestCase("firstname,lastname,username,username")]
    [TestCase("firstname,lastname,username,user_name")]
    [TestCase("firstname,first_name,lastname,username")]
    public void Dvosmislene_Kolone_Se_Odbijaju(string header)
    {
        Assert.Throws<ImportFileRejectedException>(() => new StudentListCsvParser().Parse(
            header + "\nAna,Anic,ana.anic,other.user\n"));
    }

    private sealed class BadClassifier(string mode) : IImportRowClassifier
    {
        public Task<IReadOnlyList<ImportRowResult>> ClassifyAsync(IReadOnlyList<ParsedStudentRow> rows,
            Guid offeringId, CancellationToken cancellationToken = default)
        {
            var good = new ImportRowResult(rows[0].RowNumber, rows[0].Username, RowStatus.Ispravan, "");
            IReadOnlyList<ImportRowResult> result = mode switch
            {
                "missing" => Array.Empty<ImportRowResult>(),
                "repeated" => new[] { good, good },
                "identity" => new[] { good with { Username = "other.user" } },
                "status" => new[] { good with { Status = (RowStatus)999 } },
                "error" => new[] { good with { RowNumber = 9999, Status = RowStatus.Greska } },
                _ => new[] { good with { RowNumber = 9999, Status = RowStatus.Duplikat } }
            };
            return Task.FromResult(result);
        }
    }

    [TestCase("missing")]
    [TestCase("repeated")]
    [TestCase("identity")]
    [TestCase("status")]
    [TestCase("error")]
    [TestCase("duplicate")]
    public async Task Ceo_Izlaz_Klasifikatora_Se_Proverava_Pre_Izbora(string mode)
    {
        var f = new StudentImportFixture();
        var handler = new ConfirmStudentImportHandler(f.AuthorizerFor(f.AdminId, AccountRoles.Administrator),
            f.OfferingGuard, f.Parser, new BadClassifier(mode), f.Processor, f.UnitOfWork, f.Clock);
        foreach (var selection in new IReadOnlyCollection<int>?[] { null, Array.Empty<int>() })
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await handler.HandleAsync(f.OfferingId,
                "firstname,lastname,username\nAna,Anic,ana.anic\n", selection));
            Assert.That(await f.Accounts.FindByUsernameAsync("ana.anic"), Is.Null);
        }
        Assert.That(f.Enrollments.Count, Is.Zero);
        Assert.That(f.Activations.ActivationRequested, Is.Empty);
    }

    [Test]
    public async Task Greska_U_Imenu_Ne_Sprecava_Kasniji_Ispravan_Red()
    {
        var f = new StudentImportFixture();
        const string csv = "firstname,lastname,username\n,,ana.anic\nAna,Anic,ana.anic\n";
        var preview = await f.PreviewFor(f.AdminId, AccountRoles.Administrator).HandleAsync(f.OfferingId, csv);
        Assert.That(preview.Rows.Select(r => r.Status), Is.EqualTo(new[] { RowStatus.Greska, RowStatus.Ispravan }));
        Assert.That(await f.Accounts.FindByUsernameAsync("ana.anic"), Is.Null);
        var result = await f.ConfirmFor(f.AdminId, AccountRoles.Administrator).HandleAsync(f.OfferingId, csv, new[] { 3 });
        Assert.That(result.CreatedAccountCount, Is.EqualTo(1));
        Assert.That(result.CreatedMembershipCount, Is.EqualTo(1));
        Assert.That(f.Enrollments.Count, Is.EqualTo(1));
    }

    [TestCase("missing")]
    [TestCase("repeated")]
    [TestCase("identity")]
    [TestCase("status")]
    [TestCase("error")]
    [TestCase("duplicate")]
    public void Predlog_Takodje_Proverava_Ugovor_Klasifikatora(string mode)
    {
        var f = new StudentImportFixture();
        var handler = new PreviewStudentImportHandler(f.AuthorizerFor(f.AdminId, AccountRoles.Administrator),
            f.OfferingGuard, f.Parser, new BadClassifier(mode));
        Assert.ThrowsAsync<InvalidOperationException>(async () => await handler.HandleAsync(f.OfferingId,
            "firstname,lastname,username\nAna,Anic,ana.anic\n"));
        Assert.That(f.Enrollments.Count, Is.Zero);
        Assert.That(f.Activations.ActivationRequested, Is.Empty);
    }

    [Test]
    public async Task Pad_Posle_Aktivacije_Clanstva_Vraca_Isti_Prekinuti_Zapis()
    {
        var f = new StudentImportFixture();
        var id = Guid.NewGuid();
        f.Accounts.Seed("ana.anic", id);
        f.Enrollments.Seed(id, f.OfferingId, EnrollmentState.Prekinuto);
        var before = f.Enrollments.All.Single();
        Assert.ThrowsAsync<InvalidOperationException>(async () => await f.UnitOfWork.ExecuteAsync<int>(async ct => {
            await f.Processor.ProcessAsync(new ParsedStudentRow(2, "ana.anic", "Ana", "Anic", ""), f.OfferingId, f.Clock.UtcNow, ct);
            throw new InvalidOperationException("failure");
        }));
        Assert.That(f.Enrollments.All.Single(), Is.EqualTo(before));
        Assert.That(await f.Enrollments.FindActiveAsync(id, f.OfferingId), Is.Null);
    }

    [Test]
    public async Task Paralelna_Potvrda_Prekinutog_Clanstva_Ne_Duplira_Zapis()
    {
        var f = new StudentImportFixture();
        var id = Guid.NewGuid();
        f.Accounts.Seed("ana.anic", id);
        f.Enrollments.Seed(id, f.OfferingId, EnrollmentState.Prekinuto);
        var before = f.Enrollments.All.Single();
        const string csv = "username\nana.anic\n";
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(async () => {
            var unit = new InMemoryUnitOfWork(f.Accounts, f.Activations, f.Enrollments);
            var handler = new ConfirmStudentImportHandler(f.AuthorizerFor(f.AdminId, AccountRoles.Administrator),
                f.OfferingGuard, f.Parser, f.Classifier, f.Processor, unit, f.Clock);
            return await handler.HandleAsync(f.OfferingId, csv);
        }))).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.That(results.Sum(r => r.CreatedMembershipCount), Is.EqualTo(1));
        Assert.That(results.Sum(r => r.DuplicateCount), Is.EqualTo(7));
        Assert.That(f.Enrollments.Count, Is.EqualTo(1));
        Assert.That(f.Enrollments.All.Single().EnrollmentId, Is.EqualTo(before.EnrollmentId));
        Assert.That(f.Activations.ActivationRequested, Is.Empty);
    }

    [Test]
    public async Task Druga_Potvrda_Posle_Rollback_Kreira_Sopstveni_Nalog_Bez_Siroceta()
    {
        var f = new StudentImportFixture();
        var secondOffering = Guid.NewGuid();
        f.Offerings.Seed(new OfferingSnapshot(secondOffering, f.CourseId, f.AcademicYearId, OfferingState.Aktivno, false));
        var created = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = f.UnitOfWork.ExecuteAsync<int>(async ct => {
            await f.Processor.ProcessAsync(new ParsedStudentRow(2, "ana.anic", "Ana", "Anic", ""), f.OfferingId, f.Clock.UtcNow, ct);
            created.TrySetResult();
            await release.Task.WaitAsync(ct);
            throw new InvalidOperationException("creator fails");
        });
        var other = new InMemoryUnitOfWork(f.Accounts, f.Activations, f.Enrollments);
        var handler = new ConfirmStudentImportHandler(f.AuthorizerFor(f.AdminId, AccountRoles.Administrator),
            f.OfferingGuard, f.Parser, f.Classifier, f.Processor, other, f.Clock);
        Task<ImportConfirmationResult>? second = null;
        Exception? failure = null;
        try
        {
            await created.Task.WaitAsync(TimeSpan.FromSeconds(5));
            second = handler.HandleAsync(secondOffering, "firstname,lastname,username\nAna,Anic,ana.anic\n");
            Assert.That(second.IsCompleted, Is.False);
        }
        finally
        {
            release.TrySetResult();
            try { await first.WaitAsync(TimeSpan.FromSeconds(5)); }
            catch (InvalidOperationException error) { failure = error; }
            if (second is not null) await second.WaitAsync(TimeSpan.FromSeconds(5));
        }
        Assert.That(failure, Is.TypeOf<InvalidOperationException>());
        var result = await second!;
        var account = await f.Accounts.FindByUsernameAsync("ana.anic");
        Assert.That(account, Is.Not.Null);
        Assert.That(result.CreatedAccountCount, Is.EqualTo(1));
        Assert.That(result.CreatedMembershipCount, Is.EqualTo(1));
        Assert.That(f.Enrollments.All.Single().AccountId, Is.EqualTo(account!.AccountId));
        Assert.That(f.Enrollments.All.Single().OfferingId, Is.EqualTo(secondOffering));
        Assert.That(f.Activations.ActivationRequested, Is.EqualTo(new[] { account.AccountId }));
        var replay = await handler.HandleAsync(secondOffering, "username\nana.anic\n");
        Assert.That(replay.CreatedMembershipCount, Is.Zero);
        Assert.That(replay.CreatedAccountCount, Is.Zero);
        Assert.That(replay.DuplicateCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Uhvacen_Ugnezdeni_Pad_Ipak_Ponistava_Celinu()
    {
        var accounts = new InMemoryStudentAccountDirectory();
        var unit = new InMemoryUnitOfWork(accounts);
        Assert.ThrowsAsync<InvalidOperationException>(async () => await unit.ExecuteAsync(async ct => {
            try
            {
                await unit.ExecuteAsync<int>(_ => {
                    accounts.Seed("nested.user", Guid.NewGuid());
                    throw new InvalidOperationException("nested failure");
                });
            }
            catch (InvalidOperationException) { }
            return 1;
        }));
        Assert.That(await accounts.FindByUsernameAsync("nested.user"), Is.Null);
        Assert.That(await unit.ExecuteAsync(ct => Task.FromResult(7)), Is.EqualTo(7));
    }

    [Test]
    public async Task Nasledjeni_Kontekst_Ne_Otvara_Posao_Posle_Zavrsetka_Roditelja()
    {
        var unit = new InMemoryUnitOfWork(new Participant());
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task? child = null;
        Exception? failure = null;
        try
        {
            await unit.ExecuteAsync(ct => {
                child = Task.Run(async () => {
                    await release.Task;
                    await unit.ExecuteAsync(_ => Task.FromResult(1));
                });
                return Task.FromResult(0);
            });
        }
        finally
        {
            release.TrySetResult();
            if (child is not null)
            {
                try { await child.WaitAsync(TimeSpan.FromSeconds(5)); }
                catch (InvalidOperationException error) { failure = error; }
            }
        }
        Assert.That(failure, Is.TypeOf<InvalidOperationException>());
        Assert.That(await unit.ExecuteAsync(ct => Task.FromResult(2)), Is.EqualTo(2));
    }

    [Test]
    public async Task Nov_Ucesnik_U_Ugnezdenom_Poslu_Se_Odbija_Bez_Cekanja()
    {
        var outer = new InMemoryUnitOfWork(new Participant());
        var inner = new InMemoryUnitOfWork(new Participant());
        Assert.ThrowsAsync<InvalidOperationException>(async () => await outer.ExecuteAsync(async ct =>
            await inner.ExecuteAsync(_ => Task.FromResult(1))).WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.That(await outer.ExecuteAsync(ct => Task.FromResult(1)), Is.EqualTo(1));
    }

    private sealed class Participant(bool failBegin = false, bool failRollback = false) : InMemoryTransaction
    {
        public int Begun, RolledBack;
        public void Begin() { if (failBegin) throw new InvalidOperationException("begin"); Begun++; }
        public void Commit() { }
        public void Rollback() { RolledBack++; if (failRollback) throw new InvalidOperationException("rollback"); }
    }

    [Test]
    public void Neuspesan_Begin_Vraca_Prethodne_Ucesnike()
    {
        var first = new Participant();
        var unit = new InMemoryUnitOfWork(first, new Participant(failBegin: true));
        Assert.ThrowsAsync<InvalidOperationException>(async () => await unit.ExecuteAsync(ct => Task.FromResult(1)));
        Assert.That(first.RolledBack, Is.EqualTo(1));
    }

    [Test]
    public void Rollback_Pokusava_Sve_Ucesnike_I_Cuva_Prvu_Gresku()
    {
        var first = new Participant();
        var second = new Participant(failRollback: true);
        var unit = new InMemoryUnitOfWork(first, second);
        var ex = Assert.ThrowsAsync<AggregateException>(async () => await unit.ExecuteAsync<int>(
            ct => throw new InvalidOperationException("original")));
        Assert.That(first.RolledBack, Is.EqualTo(1));
        Assert.That(ex!.InnerExceptions[0].Message, Is.EqualTo("original"));
        Assert.That(ex.InnerExceptions[1].Message, Is.EqualTo("rollback"));
    }

    [Test]
    public void Ucesnici_Se_Proveravaju_I_Niz_Se_Kopira()
    {
        var p = new Participant();
        Assert.Throws<ArgumentException>(() => new InMemoryUnitOfWork(p, p));
        Assert.Throws<ArgumentException>(() => new InMemoryUnitOfWork(new InMemoryTransaction[] { null! }));
        var participants = new InMemoryTransaction[] { p };
        var unit = new InMemoryUnitOfWork(participants);
        participants[0] = new Participant(failBegin: true);
        Assert.DoesNotThrowAsync(async () => await unit.ExecuteAsync(ct => Task.FromResult(1)));
        Assert.That(p.Begun, Is.EqualTo(1));
    }

    [Test]
    public async Task Otkazivanje_Pre_Commit_Ponistava_Upis()
    {
        var accounts = new InMemoryStudentAccountDirectory();
        var unit = new InMemoryUnitOfWork(accounts);
        using var cts = new CancellationTokenSource();
        Assert.ThrowsAsync<OperationCanceledException>(async () => await unit.ExecuteAsync(ct =>
        {
            accounts.Seed("cancel.user", Guid.NewGuid());
            cts.Cancel();
            return Task.FromResult(1);
        }, cts.Token));
        Assert.That(await accounts.FindByUsernameAsync("cancel.user"), Is.Null);
    }

}
