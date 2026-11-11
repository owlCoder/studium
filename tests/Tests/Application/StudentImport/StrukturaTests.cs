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
