using Application.StudentImport;
using NUnit.Framework;

namespace Tests.Application.StudentImport;

[TestFixture]
public sealed class PreviewStudentImportHandlerTests
{
    private StudentImportFixture _fixture = null!;

    [SetUp]
    public void SetUp() => _fixture = new StudentImportFixture();

    private PreviewStudentImportHandler AsAdmin()
        => _fixture.PreviewFor(_fixture.AdminId, AccountRoles.Administrator);

    [Test]
    public async Task Primer_Iz_Specifikacije_Daje_17_2_1()
    {
        var preview = await AsAdmin().HandleAsync(
            _fixture.OfferingId, StudentImportFixture.SpecExample);

        Assert.That(preview.TotalCount, Is.EqualTo(20));
        Assert.That(preview.CorrectCount, Is.EqualTo(17));
        Assert.That(preview.DuplicateCount, Is.EqualTo(2));
        Assert.That(preview.ErrorCount, Is.EqualTo(1));
        Assert.That(preview.CorrectCount + preview.DuplicateCount + preview.ErrorCount, Is.EqualTo(preview.TotalCount));
    }

    [Test]
    public async Task Predlog_Ne_Menja_Evidenciju()
    {
        var id = Guid.NewGuid();
        _fixture.Accounts.Seed("ana.anic", id, firstName: "Ana", lastName: "Anic");
        _fixture.Enrollments.Seed(id, _fixture.OfferingId, EnrollmentState.Prekinuto);
        var accountBefore = await _fixture.Accounts.FindByUsernameAsync("ana.anic");
        var membershipBefore = _fixture.Enrollments.All.Single();
        var preview = await AsAdmin().HandleAsync(_fixture.OfferingId, StudentImportFixture.SpecExample);

        Assert.That(preview.Rows, Has.Count.EqualTo(20));
        Assert.That(await _fixture.Accounts.FindByUsernameAsync("marko.markovic"), Is.Null);
        Assert.That(await _fixture.Accounts.FindByUsernameAsync("ana.anic"), Is.EqualTo(accountBefore));
        Assert.That(_fixture.Enrollments.All, Is.EqualTo(new[] { membershipBefore }));
        Assert.That(_fixture.Activations.ActivationRequested, Is.Empty);
    }

    [Test]
    public async Task Svaki_Red_Ima_Status_I_Razlog_Kada_Nije_Ispravan()
    {
        var preview = await AsAdmin().HandleAsync(
            _fixture.OfferingId, StudentImportFixture.SpecExample);

        // Ред 1 је заглавље, па подаци носе бројеве 2..21.
        Assert.That(preview.Rows.Select(r => r.RowNumber), Is.EqualTo(Enumerable.Range(2, 20)));
        Assert.That(preview.Rows.Single(r => r.RowNumber == 21).Status, Is.EqualTo(RowStatus.Greska));
        Assert.That(
            preview.Rows.Where(r => r.Status != RowStatus.Ispravan).All(r => r.Reason.Length > 0),
            Is.True);
    }

    [Test]
    public async Task Red_Bez_Korisnickog_Imena_Je_Greska()
    {
        var preview = await AsAdmin().HandleAsync(
            _fixture.OfferingId, "firstname,lastname,username\nAna,Anić,\n");

        var row = preview.Rows.Single();
        Assert.That(row.Status, Is.EqualTo(RowStatus.Greska));
        Assert.That(row.Reason, Does.Contain("korisnicko ime"));
    }

    [Test]
    public async Task Prazno_Korisnicko_Ime_Sa_Razmacima_Je_Greska()
    {
        var preview = await AsAdmin().HandleAsync(
            _fixture.OfferingId, "firstname,lastname,username\nAna,Anić,   \n");

        Assert.That(preview.Rows.Single().Status, Is.EqualTo(RowStatus.Greska));
    }

    [TestCase("Ana", "")]
    [TestCase("", "Anic")]
    [TestCase("", "")]
    public async Task Nedostajuce_Ime_Ili_Prezime_Je_Greska_Jer_Ih_ST001_Zahteva(string first, string last)
    {
        var preview = await AsAdmin().HandleAsync(
            _fixture.OfferingId, $"firstname,lastname,username\n{first},{last},ana.anic\n");

        var row = preview.Rows.Single();
        Assert.That(row.Status, Is.EqualTo(RowStatus.Greska));
        Assert.That(row.Reason, Does.Contain("Ime i prezime"));
    }

    [Test]
    public async Task Neispravna_Adresa_Je_Greska_A_Odsutna_Je_Ispravna()
    {
        var preview = await AsAdmin().HandleAsync(
            _fixture.OfferingId,
            "firstname,lastname,username,email\nAna,Anić,ana.anic,ana@\nMarko,Marković,marko.markovic,\n");

        Assert.That(preview.Rows[0].Status, Is.EqualTo(RowStatus.Greska));
        Assert.That(preview.Rows[0].Reason, Does.Contain("elektronske poste"));
        Assert.That(preview.Rows[1].Status, Is.EqualTo(RowStatus.Ispravan));
    }

    [Test]
    public async Task Neispravna_Tacka_U_Domenu_EPoste_Je_Greska()
    {
        var preview = await AsAdmin().HandleAsync(
            _fixture.OfferingId,
            "firstname,lastname,username,email\n" +
            "Ana,Anić,ana.anic,ana@.example.com\n" +
            "Marko,Marković,marko.markovic,marko@example..com\n");

        Assert.That(preview.Rows[0].Status, Is.EqualTo(RowStatus.Greska));
        Assert.That(preview.Rows[1].Status, Is.EqualTo(RowStatus.Greska));
    }

    [Test]
    public async Task Samo_Kolona_Imena_Bez_Prezimena_Daje_Tacno_Upozorenje()
    {
        var preview = await AsAdmin().HandleAsync(
            _fixture.OfferingId, "username,firstname\nana.anic,Ana\n");

        Assert.That(preview.Rows.Single().Status, Is.EqualTo(RowStatus.Greska));
        Assert.That(string.Join(" ", preview.Warnings), Does.Not.Contain("jednoj koloni"));
        Assert.That(string.Join(" ", preview.Warnings), Does.Contain("i ime i prezime"));
    }

    [Test]
    public async Task Deaktiviran_Nalog_Je_Greska()
    {
        _fixture.Accounts.Seed("stari.student", Guid.NewGuid(), state: AccountState.Deaktiviran);

        var preview = await AsAdmin().HandleAsync(
            _fixture.OfferingId, "firstname,lastname,username\nStari,Student,stari.student\n");

        var row = preview.Rows.Single();
        Assert.That(row.Status, Is.EqualTo(RowStatus.Greska));
        Assert.That(row.Reason, Does.Contain("deaktiviran"));
    }

    [Test]
    public async Task Nalog_Druge_Uloge_Je_Greska()
    {
        _fixture.Accounts.Seed("prof.petar", Guid.NewGuid(), role: AccountRole.Nastavnik);

        var preview = await AsAdmin().HandleAsync(
            _fixture.OfferingId, "firstname,lastname,username\nPetar,Petrović,prof.petar\n");

        var row = preview.Rows.Single();
        Assert.That(row.Status, Is.EqualTo(RowStatus.Greska));
        Assert.That(row.Reason, Does.Contain("ulogu studenta"));
    }

    [Test]
    public async Task Aktivan_Student_Bez_Zavrsene_Aktivacije_Sme_Da_Se_Upise()
    {
        _fixture.Accounts.Seed(
            "ana.anic", Guid.NewGuid(),
            role: AccountRole.Student,
            state: AccountState.Aktivan,
            isActivated: false);

        var preview = await AsAdmin().HandleAsync(
            _fixture.OfferingId, "firstname,lastname,username\nAna,Anić,ana.anic\n");

        Assert.That(preview.Rows.Single().Status, Is.EqualTo(RowStatus.Ispravan));
    }

    [Test]
    public async Task Ponavljanje_Imena_U_Datoteci_Daje_Duplikat_A_Ne_Gresku()
    {
        var preview = await AsAdmin().HandleAsync(
            _fixture.OfferingId,
            "firstname,lastname,username\nAna,Anić,ana.anic\nAna,Anić,ana.anic\n");

        Assert.That(preview.Rows[0].Status, Is.EqualTo(RowStatus.Ispravan));
        Assert.That(preview.Rows[1].Status, Is.EqualTo(RowStatus.Duplikat));
        Assert.That(preview.Rows[1].Reason, Does.Contain("redu 2"));
        Assert.That(preview.CorrectCount, Is.EqualTo(1));
        Assert.That(preview.DuplicateCount, Is.EqualTo(1));
        Assert.That(preview.ErrorCount, Is.Zero);
    }

    [Test]
    public async Task Postojece_Clanstvo_Na_Izvodjenju_Je_Duplikat()
    {
        var accountId = Guid.NewGuid();
        _fixture.Accounts.Seed("ana.anic", accountId);
        _fixture.Enrollments.Seed(accountId, _fixture.OfferingId);

        var preview = await AsAdmin().HandleAsync(
            _fixture.OfferingId, "firstname,lastname,username\nAna,Anić,ana.anic\n");

        Assert.That(preview.Rows.Single().Status, Is.EqualTo(RowStatus.Duplikat));
        Assert.That(preview.Rows.Single().Reason, Does.Contain("vec clan"));
    }

    [Test]
    public async Task Korisnicko_Ime_Se_Poredi_Bez_Razlike_U_Velikim_Slovima()
    {
        _fixture.Accounts.Seed("ana.petrovic", Guid.NewGuid());
        // Без имена нов налог не би прошао: успех тражи проналажење постојећег.
        var preview = await AsAdmin().HandleAsync(_fixture.OfferingId, "username\nAna.Petrovic\n");
        Assert.That(preview.Rows.Single().Status, Is.EqualTo(RowStatus.Ispravan));
    }

    [Test]
    public async Task Datoteka_Bez_Kolone_Korisnickog_Imena_Se_Odbija()
    {
        var handler = AsAdmin();

        var ex = Assert.ThrowsAsync<ImportFileRejectedException>(
            async () => await handler.HandleAsync(_fixture.OfferingId, "firstname,lastname\nAna,Anić\n"));

        Assert.That(ex!.Message, Does.Contain("korisnickim imenom"));
        Assert.That(await _fixture.Accounts.FindByUsernameAsync("ana.anic"), Is.Null);
    }

    [Test]
    public async Task Datoteka_Bez_Imena_I_Prezimena_Se_Prihvata_Uz_Upozorenje()
    {
        // За упис постојећег налога довољно је корисничко име.
        var preview = await AsAdmin().HandleAsync(_fixture.OfferingId, "username\nana.anic\n");

        Assert.That(preview.Rows.Single().Status, Is.EqualTo(RowStatus.Greska));
        Assert.That(string.Join(" ", preview.Warnings), Does.Contain("ime i prezime"));
        Assert.That(preview.Rows.Single().Reason, Does.Contain("Ime i prezime"));
    }

    [Test]
    public void Prazna_Datoteka_Se_Odbija()
    {
        var handler = AsAdmin();

        Assert.ThrowsAsync<ImportFileRejectedException>(
            async () => await handler.HandleAsync(_fixture.OfferingId, "   "));
    }
}
