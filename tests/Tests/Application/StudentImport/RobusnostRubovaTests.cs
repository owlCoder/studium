using Application.StudentImport;
using NUnit.Framework;

namespace Tests.Application.StudentImport;

/// <summary>Провере обима, нормализације, заглавља и празног идентитета.</summary>
[TestFixture]
public sealed class RobusnostRubovaTests
{
    private const string Csv = "firstname,lastname,username\nAna,Anić,ana.anic\n";

    /// <summary>Канонски еквивалентни Уникод записи не стварају два налога.</summary>
    [Test]
    public async Task Isto_Ime_U_Dva_Unicode_Zapisa_Je_Duplikat()
    {
        var fixture = new StudentImportFixture();
        var nfc = "đorđe.đorđević".Normalize(System.Text.NormalizationForm.FormC);
        var nfd = "đorđe.đorđević".Normalize(System.Text.NormalizationForm.FormD);

        var preview = await fixture.PreviewFor(fixture.AdminId, AccountRoles.Administrator)
            .HandleAsync(fixture.OfferingId,
                $"firstname,lastname,username\nĐorđe,Đorđević,{nfc}\nĐorđe,Đorđević,{nfd}\n");

        Assert.That(preview.Rows[0].Status, Is.EqualTo(RowStatus.Ispravan));
        Assert.That(preview.Rows[1].Status, Is.EqualTo(RowStatus.Duplikat));
    }

    /// <summary>Провера целог увоза на већем списку.</summary>
    [Test]
    public async Task Uvoz_Tri_Hiljade_Redova_Cuva_Sva_Clanstva()
    {
        var fixture = new StudentImportFixture();
        var csv = new System.Text.StringBuilder("firstname,lastname,username\n");
        for (var i = 0; i < 3000; i++) csv.Append($"Ime{i},Prezime{i},student.{i}\n");

        var result = await fixture.ConfirmFor(fixture.AdminId, AccountRoles.Administrator)
            .HandleAsync(fixture.OfferingId, csv.ToString());

        Assert.That(result.CreatedMembershipCount, Is.EqualTo(3000));
        Assert.That(fixture.Enrollments.Count, Is.EqualTo(3000));
        Assert.That(fixture.Activations.ActivationRequested.Distinct().Count(), Is.EqualTo(3000));
    }

    /// <summary>Избор из већег списка уписује само означене редове.</summary>
    [Test]
    public async Task Izbor_Iz_Veceg_Spiska_Upisuje_Samo_Oznacene()
    {
        var fixture = new StudentImportFixture();
        var csv = new System.Text.StringBuilder("firstname,lastname,username\n");
        for (var i = 0; i < 2000; i++) csv.Append($"Ime{i},Prezime{i},student.{i}\n");

        var result = await fixture.ConfirmFor(fixture.AdminId, AccountRoles.Administrator)
            .HandleAsync(fixture.OfferingId, csv.ToString(), Enumerable.Range(2, 700).ToList());

        Assert.That(result.CreatedMembershipCount, Is.EqualTo(700));
        Assert.That(fixture.Enrollments.Count, Is.EqualTo(700));
        Assert.That(await fixture.Accounts.FindByUsernameAsync("student.699"), Is.Not.Null);
        Assert.That(await fixture.Accounts.FindByUsernameAsync("student.700"), Is.Null);
    }

    [Test]
    public void Razdeljivac_Se_Bira_Van_Navodnika()
    {
        // Назив колоне садржи тачку-зарез; раздељивач је ипак зарез.
        var document = new CsvReader().Read("\"prvo;kolona\",drugo\n1,2\n");

        Assert.That(document.Delimiter, Is.EqualTo(','));
        Assert.That(document.Header.Cells, Is.EqualTo(new[] { "prvo;kolona", "drugo" }));
    }

    [Test]
    public void Podrazumevani_Identitet_Nije_Prijavljen_Korisnik()
    {
        var fixture = new StudentImportFixture();
        var authorizer = fixture.AuthorizerFor(Guid.Empty, AccountRoles.Administrator);

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await authorizer.EnsureCanImportAsync(fixture.OfferingId));
    }

    [Test]
    public async Task Potvrda_Ponovo_Proverava_Stanje_Naloga()
    {
        var fixture = new StudentImportFixture();

        // Налог је деактивиран после предлога, а пре потврде.
        var preview = await fixture.PreviewFor(fixture.AdminId, AccountRoles.Administrator)
            .HandleAsync(fixture.OfferingId, Csv);
        fixture.Accounts.Seed("ana.anic", Guid.NewGuid(), state: AccountState.Deaktiviran);

        var result = await fixture.ConfirmFor(fixture.AdminId, AccountRoles.Administrator)
            .HandleAsync(fixture.OfferingId, Csv);

        Assert.That(preview.Rows[0].Status, Is.EqualTo(RowStatus.Ispravan));
        Assert.That(result.ErrorCount, Is.EqualTo(1));
        Assert.That(result.CreatedMembershipCount, Is.Zero);
    }

    [Test]
    public async Task Nepostojeci_Broj_Reda_U_Izboru_Se_Ignorise()
    {
        var fixture = new StudentImportFixture();

        var result = await fixture.ConfirmFor(fixture.AdminId, AccountRoles.Administrator)
            .HandleAsync(fixture.OfferingId, Csv, new[] { 2, 99, 1000 });

        Assert.That(result.CreatedMembershipCount, Is.EqualTo(1));
    }

    [Test]
    public void Datoteka_Bez_Zaglavlja_Se_Odbija_A_Ne_Tumaci_Kao_Podatke()
    {
        // Први ред је заглавље по конвенцији; без њега датотека није употребљива.
        var parser = new StudentListCsvParser();

        Assert.That(
            () => parser.Parse("Ana,Anić,ana.anic\nMarko,Marković,marko.markovic\n"),
            Throws.TypeOf<ImportFileRejectedException>());
    }

    [Test]
    public void Zaglavlje_Sa_Navodnicima_Se_Cita_Isto()
    {
        var parsed = new StudentListCsvParser()
            .Parse("\"firstname\",\"lastname\",\"username\"\nAna,Anić,ana.anic\n");

        Assert.That(parsed.Rows.Single().Username, Is.EqualTo("ana.anic"));
    }

    [Test]
    public void Vrlo_Dugo_Polje_Se_Ne_Oseca_Napola()
    {
        var longName = new string('a', 5000);
        var row = new StudentListCsvParser()
            .Parse($"firstname,lastname,username\n{longName},Anić,ana.anic\n").Rows.Single();

        Assert.That(row.FirstName, Has.Length.EqualTo(5000));
    }
}
