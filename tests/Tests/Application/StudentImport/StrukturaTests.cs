using Application.StudentImport;
using NUnit.Framework;

namespace Tests.Application.StudentImport;

/// <summary>Проверава замену портова и додавање правила без измене постојећих класа.</summary>
[TestFixture]
public sealed class StrukturaTests
{
    /// <summary>Приватно правило које одбија задато корисничко име.</summary>
    private sealed class RejectUsernameRule : IImportRowRule
    {
        public Task<ImportRowResult?> EvaluateAsync(
            ParsedStudentRow row, Guid offeringId, CancellationToken cancellationToken)
            => Task.FromResult(
                row.Username.StartsWith("test.", StringComparison.OrdinalIgnoreCase)
                    ? (ImportRowResult?)new ImportRowResult(
                        row.RowNumber, row.Username, RowStatus.Greska, "Zabranjen prefiks.")
                    : null);
    }

    /// <summary>Тестно правило које чита налог преко <see cref="IAccountAwareRule"/>.</summary>
    private sealed class RejectUnconfirmedExistingEmailRule : IAccountAwareRule
    {
        public IAccountDirectory Accounts { get; }

        public RejectUnconfirmedExistingEmailRule(IAccountDirectory accounts)
            => Accounts = accounts;

        public IImportRowRule WithAccounts(IAccountDirectory accounts)
            => new RejectUnconfirmedExistingEmailRule(accounts);

        public async Task<ImportRowResult?> EvaluateAsync(
            ParsedStudentRow row, Guid offeringId, CancellationToken cancellationToken)
        {
            var account = await Accounts.FindByUsernameAsync(row.Username, cancellationToken);
            return account is not null && account.Email is not null && !account.IsEmailConfirmed
                ? new ImportRowResult(row.RowNumber, row.Username, RowStatus.Greska, "Nepotvrdjena e-posta.")
                : null;
        }
    }

    private sealed class StubAuthorizer : IStudentImportAuthorizer
    {
        public Task EnsureCanImportAsync(Guid offeringId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class StubOfferingGuard : IStudentOfferingGuard
    {
        public Task<OfferingSnapshot> EnsureExistsAsync(Guid offeringId, CancellationToken cancellationToken = default)
            => Task.FromResult(new OfferingSnapshot(
                offeringId, Guid.NewGuid(), Guid.NewGuid(), OfferingState.Aktivno, false));
    }

    private sealed class StubParser : IStudentListParser
    {
        public ParsedStudentList Parse(string csvText)
            => new(
                new[] { new ParsedStudentRow(2, "stub.user", "Stub", "User", string.Empty) },
                Array.Empty<string>());
    }

    private sealed class StubClassifier : IImportRowClassifier
    {
        public Task<IReadOnlyList<ImportRowResult>> ClassifyAsync(
            IReadOnlyList<ParsedStudentRow> rows, Guid offeringId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ImportRowResult>>(
                rows.Select(r => new ImportRowResult(r.RowNumber, r.Username, RowStatus.Ispravan, string.Empty))
                    .ToList());
    }

    private sealed class StubFileReader : IStudentListFileReader
    {
        public Task<string> ReadAsync(Stream stream, CancellationToken cancellationToken = default)
            => Task.FromResult("ignored");
    }

    private sealed class StubRowProcessor : IImportRowProcessor
    {
        public Task<RowWriteResult> ProcessAsync(
            ParsedStudentRow row, Guid offeringId, DateTimeOffset enrolledAt, CancellationToken cancellationToken = default)
            => Task.FromResult(new RowWriteResult(RowWriteOutcome.Written, true));
    }

    [Test]
    public async Task Novo_Pravilo_Se_Dodaje_Bez_Izmene_Klasifikatora()
    {
        var fixture = new StudentImportFixture();

        var classifier = new ImportRowClassifier(
            new IImportRowRule[] { new UsernameRequiredRule(), new RejectUsernameRule() },
            new RepeatedUsernameRule(),
            new IImportRowRule[]
            {
                new AccountEligibilityRule(fixture.Accounts),
                new ExistingEnrollmentRule(fixture.Accounts, fixture.Enrollments),
            });

        var rows = new[]
        {
            new ParsedStudentRow(2, "test.ana", "Ana", "Anić", string.Empty),
            new ParsedStudentRow(3, "ana.anic", "Ana", "Anić", string.Empty),
        };

        var results = await classifier.ClassifyAsync(rows, fixture.OfferingId);

        Assert.That(results[0].Status, Is.EqualTo(RowStatus.Greska));
        Assert.That(results[0].Reason, Is.EqualTo("Zabranjen prefiks."));
        Assert.That(results[1].Status, Is.EqualTo(RowStatus.Ispravan));
    }

    [Test]
    public async Task Novo_Pravilo_Sa_Nalozima_Automatski_Koristi_Kes_Bez_Izmene_Klasifikatora()
    {
        var fixture = new StudentImportFixture();
        fixture.Accounts.Seed(
            "ana.anic",
            Guid.NewGuid(),
            email: "ana@example.com",
            isEmailConfirmed: false);

        var classifier = new ImportRowClassifier(
            new IImportRowRule[] { new UsernameRequiredRule() },
            new RepeatedUsernameRule(),
            new IImportRowRule[]
            {
                new AccountEligibilityRule(fixture.Accounts),
                new ExistingEnrollmentRule(fixture.Accounts, fixture.Enrollments),
                new RejectUnconfirmedExistingEmailRule(fixture.Accounts),
            });

        var results = await classifier.ClassifyAsync(
            new[] { new ParsedStudentRow(2, "ana.anic", "Ana", "Anić", "ana@example.com") },
            fixture.OfferingId);

        // Сва три правила питала су исто корисничко име, али је складиште питано тачно једном.
        Assert.That(results.Single().Status, Is.EqualTo(RowStatus.Greska));
        Assert.That(results.Single().Reason, Is.EqualTo("Nepotvrdjena e-posta."));
        Assert.That(fixture.Accounts.LookupCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Handleri_Prihvataju_Zamenske_Portove()
    {
        var fixture = new StudentImportFixture();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        var previewHandler = new PreviewStudentImportHandler(
            new StubAuthorizer(),
            new StubOfferingGuard(),
            new StubParser(),
            new StubClassifier(),
            new StubFileReader());

        var preview = await previewHandler.HandleAsync(fixture.OfferingId, stream);
        Assert.That(preview.CorrectCount, Is.EqualTo(1));

        stream.Position = 0;
        var confirmHandler = new ConfirmStudentImportHandler(
            new StubAuthorizer(),
            new StubOfferingGuard(),
            new StubParser(),
            new StubClassifier(),
            new StubRowProcessor(),
            fixture.UnitOfWork,
            fixture.Clock,
            new StubFileReader());

        var confirmation = await confirmHandler.HandleAsync(fixture.OfferingId, stream);
        Assert.That(confirmation.CreatedAccountCount, Is.EqualTo(1));
        Assert.That(confirmation.CreatedMembershipCount, Is.EqualTo(1));
    }

    private sealed class ReadThenAddThenReadRule : IAccountAwareRule
    {
        public IAccountDirectory Accounts { get; }
        public AccountSnapshot? SecondLookup { get; private set; }

        public ReadThenAddThenReadRule(IAccountDirectory accounts) => Accounts = accounts;

        public IImportRowRule WithAccounts(IAccountDirectory accounts)
            => new ReadThenAddThenReadRule(accounts);

        public async Task<ImportRowResult?> EvaluateAsync(
            ParsedStudentRow row, Guid offeringId, CancellationToken cancellationToken)
        {
            var first = await Accounts.FindByUsernameAsync(row.Username, cancellationToken);
            if (first is not null) return null;

            await Accounts.TryAddAsync(new NewUserAccount(
                row.FirstName, row.LastName, row.Username,
                AccountRole.Student, AccountState.Aktivan, IsActivated: false,
                Email: null, IsEmailConfirmed: false), cancellationToken);

            SecondLookup = await Accounts.FindByUsernameAsync(row.Username, cancellationToken);
            return SecondLookup is null
                ? new ImportRowResult(row.RowNumber, row.Username, RowStatus.Greska, "Zastareo kes.")
                : null;
        }
    }

    [Test]
    public async Task Kes_Naloga_Ne_Vraca_Zastareo_Odgovor_Posle_Upisa()
    {
        var fixture = new StudentImportFixture();
        var rule = new ReadThenAddThenReadRule(fixture.Accounts);

        var classifier = new ImportRowClassifier(
            new IImportRowRule[] { new UsernameRequiredRule() },
            new RepeatedUsernameRule(),
            new IImportRowRule[] { rule });

        var results = await classifier.ClassifyAsync(
            new[] { new ParsedStudentRow(2, "ana.anic", "Ana", "Anić", string.Empty) },
            fixture.OfferingId);

        Assert.That(results.Single().Status, Is.EqualTo(RowStatus.Ispravan));
    }

    [Test]
    public async Task Klasifikator_Sa_Samo_Lokalnim_Pravilima_Radi_Bez_Naloga()
    {
        var classifier = new ImportRowClassifier(
            new IImportRowRule[] { new UsernameRequiredRule() },
            new RepeatedUsernameRule(),
            Array.Empty<IImportRowRule>());

        var results = await classifier.ClassifyAsync(
            new[] { new ParsedStudentRow(2, "ana.anic", "Ana", "Anić", string.Empty) },
            Guid.NewGuid());

        Assert.That(results.Single().Status, Is.EqualTo(RowStatus.Ispravan));
    }

    [Test]
    public async Task Procesor_Razlikuje_Novi_Nalog_Od_Postojeceg()
    {
        var fixture = new StudentImportFixture();
        fixture.Accounts.Seed("postoji.vec", Guid.NewGuid());

        var existing = await fixture.Processor.ProcessAsync(
            new ParsedStudentRow(2, "postoji.vec", "Već", "Postoji", null!), fixture.OfferingId,
            StudentImportFixture.Now);

        var fresh = await fixture.Processor.ProcessAsync(
            new ParsedStudentRow(3, "nova.student", "Nova", "Student", null!), fixture.OfferingId,
            StudentImportFixture.Now);

        Assert.That(existing.Outcome, Is.EqualTo(RowWriteOutcome.Written));
        Assert.That(existing.CreatedAccount, Is.False);
        Assert.That(fresh.Outcome, Is.EqualTo(RowWriteOutcome.Written));
        Assert.That(fresh.CreatedAccount, Is.True);
        Assert.That(fixture.Activations.ActivationRequested, Has.Count.EqualTo(1));
    }

    [Test]
    public void Nazivi_Kolona_Prate_Oblik_Korisnika_Iz_ST001()
    {
        // Исти подаци, два записа истог облика: енглески и српски назив поља.
        var english = new StudentListCsvParser().Parse(
            "username,firstname,lastname,email\nana.anic,Ana,Anić,ana@example.com\n");

        var serbian = new StudentListCsvParser().Parse(
            "username,ime,prezime,email\nana.anic,Ana,Anić,ana@example.com\n");

        Assert.That(english.Rows.Single(), Is.EqualTo(serbian.Rows.Single()));
        Assert.That(english.Rows.Single().FirstName, Is.EqualTo("Ana"));
    }

    [Test]
    public void Nepoznat_Naziv_Kolone_Se_Ne_Pogadja()
    {
        // Непознато заглавље се одбија.
        var parser = new StudentListCsvParser();

        Assert.That(
            () => parser.Parse("nadimak,firstname,lastname\nana,Ana,Anić\n"),
            Throws.TypeOf<ImportFileRejectedException>());
    }

    [Test]
    public void Citanje_Formata_Ne_Zna_Nista_O_Kolonama()
    {
        var document = new CsvReader().Read("a;b\n1;2\n");

        Assert.That(document.Delimiter, Is.EqualTo(';'));
        Assert.That(document.Header.Cells, Is.EqualTo(new[] { "a", "b" }));
        Assert.That(document.DataRows.Single().Cells, Is.EqualTo(new[] { "1", "2" }));
    }
}

[TestFixture]
public sealed class PrazniRedoviTests
{
    /// <summary>Празни редови се прескачу без промене физичких бројева осталих редова.</summary>
    [Test]
    public void Prazan_Red_Se_Preskace_A_Brojevi_Ostaju_Fizicki()
    {
        const string csv =
            "firstname,lastname,username\n" +
            "Ana,Anić,ana.anic\n" +
            "\n" +
            "Marko,Marković,marko.markovic\n";

        var document = new CsvReader().Read(csv);

        Assert.That(document.Rows, Has.Count.EqualTo(3));          // заглавље + 2 реда
        Assert.That(document.Rows.Select(r => r.Number), Is.EqualTo(new[] { 1, 2, 4 }));
    }
}

[TestFixture]
public sealed class PredlogZbiroviTests
{
    /// <summary>Само заглавље даје празан предлог, не грешку.</summary>
    [Test]
    public async Task Datoteka_Sa_Samo_Zaglavljem_Ima_Nula_Redova()
    {
        var fixture = new StudentImportFixture();

        var preview = await fixture.PreviewFor(fixture.AdminId, AccountRoles.Administrator)
            .HandleAsync(fixture.OfferingId, "firstname,lastname,username\n");

        Assert.That(preview.Rows, Is.Empty);
        Assert.That(preview.TotalCount, Is.Zero);
        Assert.That(preview.CorrectCount, Is.Zero);
    }
}

[TestFixture]
public sealed class BrojUpitaTests
{
    /// <summary>Класификација чита налог једном по корисничком имену.</summary>
    [Test]
    public async Task Predlog_Pita_Nalog_Jednom_Po_Korisnickom_Imenu()
    {
        var fixture = new StudentImportFixture();

        var preview = await fixture.PreviewFor(fixture.AdminId, AccountRoles.Administrator)
            .HandleAsync(fixture.OfferingId, StudentImportFixture.SpecExample);

        var distinctUsernames = preview.Rows
            .Select(r => r.Username)
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        Assert.That(fixture.Accounts.LookupCount, Is.EqualTo(distinctUsernames));
        Assert.That(fixture.Accounts.LookupCount, Is.LessThan(preview.TotalCount));
    }
}

[TestFixture]
public sealed class ZbiroviNaIzboruTests
{
    private const string Csv =
        "firstname,lastname,username\n" +
        "Ana,Anić,ana.anic\n" +      // ред 2 — исправан
        "Ana,Anić,ana.anic\n" +      // ред 3 — дупликат
        "Bez,,bez.ime\n";            // ред 4 — грешка

    /// <summary>Бројачи потврде обухватају само означене редове.</summary>
    [Test]
    public async Task Zbirovi_Obuhvataju_Samo_Oznacene_Redove()
    {
        var fixture = new StudentImportFixture();

        var result = await fixture.ConfirmFor(fixture.AdminId, AccountRoles.Administrator)
            .HandleAsync(fixture.OfferingId, Csv, new[] { 2 });

        Assert.That(result.CreatedMembershipCount, Is.EqualTo(1));
        Assert.That(result.DuplicateCount, Is.Zero);
        Assert.That(result.ErrorCount, Is.Zero);
    }

    [Test]
    public async Task Prazan_Izbor_Ne_Menja_Evidenciju()
    {
        var fixture = new StudentImportFixture();

        var result = await fixture.ConfirmFor(fixture.AdminId, AccountRoles.Administrator)
            .HandleAsync(fixture.OfferingId, Csv, Array.Empty<int>());

        Assert.That(result.CreatedMembershipCount, Is.Zero);
        Assert.That(result.CreatedAccountCount, Is.Zero);
        Assert.That(result.DuplicateCount, Is.Zero);
        Assert.That(result.ErrorCount, Is.Zero);
        Assert.That(fixture.Enrollments.Count, Is.Zero);
        Assert.That(fixture.Activations.ActivationRequested, Is.Empty);
        Assert.That(await fixture.Accounts.FindByUsernameAsync("ana.anic"), Is.Null);
    }

    /// <summary>Без избора се обрађују сви редови, па збир мора да покрије целу датотеку.</summary>
    [Test]
    public async Task Bez_Izbora_Zbir_Pokriva_Sve_Redove()
    {
        var fixture = new StudentImportFixture();

        var result = await fixture.ConfirmFor(fixture.AdminId, AccountRoles.Administrator)
            .HandleAsync(fixture.OfferingId, Csv);

        Assert.That(result.CreatedMembershipCount, Is.EqualTo(1));
        Assert.That(result.DuplicateCount, Is.EqualTo(1));
        Assert.That(result.ErrorCount, Is.EqualTo(1));
    }
}
