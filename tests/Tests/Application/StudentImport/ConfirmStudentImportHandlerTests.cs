using Application.StudentImport;
using NUnit.Framework;

namespace Tests.Application.StudentImport;

[TestFixture]
public sealed class ConfirmStudentImportHandlerTests
{
    private StudentImportFixture _fixture = null!;

    [SetUp]
    public void SetUp() => _fixture = new StudentImportFixture();

    private ConfirmStudentImportHandler AsAdmin()
        => _fixture.ConfirmFor(_fixture.AdminId, AccountRoles.Administrator);

    [Test]
    public async Task Potvrda_Stvara_Samo_Clanstva_Ispravnih_Redova()
    {
        var result = await AsAdmin().HandleAsync(
            _fixture.OfferingId, StudentImportFixture.SpecExample);

        Assert.That(result.CreatedAccountCount, Is.EqualTo(17));
        Assert.That(result.CreatedMembershipCount, Is.EqualTo(17));
        Assert.That(result.DuplicateCount, Is.EqualTo(2));
        Assert.That(result.ErrorCount, Is.EqualTo(1));
        Assert.That(_fixture.Enrollments.Count, Is.EqualTo(17));
        Assert.That(_fixture.Activations.ActivationRequested, Has.Count.EqualTo(17));
    }

    [Test]
    public async Task Svaki_Novi_Nalog_Pokrece_Zasebnu_Prvu_Aktivaciju()
    {
        await AsAdmin().HandleAsync(_fixture.OfferingId, StudentImportFixture.SpecExample);

        // Прва активација се покреће за сваки нови налог засебно.
        Assert.That(_fixture.Activations.ActivationRequested, Has.Count.EqualTo(17));
        Assert.That(_fixture.Activations.ActivationRequested.Distinct().Count(), Is.EqualTo(17));
    }

    [Test]
    public async Task Novi_Nalog_Nosi_Potpuni_Oblik_Iz_ST001_I_Nepotvrdjenu_EPostu()
    {
        await AsAdmin().HandleAsync(
            _fixture.OfferingId,
            "firstname,lastname,username,email\nAna,Anić,ana.anic,ana@example.com\nMarko,Marković,marko.markovic,\n");

        var ana = await _fixture.Accounts.FindByUsernameAsync("ana.anic");
        var marko = await _fixture.Accounts.FindByUsernameAsync("marko.markovic");

        Assert.That(ana, Is.Not.Null);
        Assert.That(ana!.FirstName, Is.EqualTo("Ana"));
        Assert.That(ana.LastName, Is.EqualTo("Anić"));
        Assert.That(ana.Username, Is.EqualTo("ana.anic"));
        Assert.That(ana.Role, Is.EqualTo(AccountRole.Student));
        Assert.That(ana.State, Is.EqualTo(AccountState.Aktivan));
        Assert.That(ana.IsActivated, Is.False);
        Assert.That(ana.Email, Is.EqualTo("ana@example.com"));
        Assert.That(ana.IsEmailConfirmed, Is.False);

        Assert.That(marko, Is.Not.Null);
        Assert.That(marko!.Email, Is.Null);
        Assert.That(marko.IsEmailConfirmed, Is.False);
    }

    [Test]
    public async Task Deaktiviran_Nalog_Ne_Dobija_Clanstvo()
    {
        var deactivatedId = Guid.NewGuid();
        _fixture.Accounts.Seed("stari.student", deactivatedId, state: AccountState.Deaktiviran);

        var result = await AsAdmin().HandleAsync(
            _fixture.OfferingId, "firstname,lastname,username\nStari,Student,stari.student\n");

        Assert.That(result.CreatedMembershipCount, Is.Zero);
        Assert.That(result.ErrorCount, Is.EqualTo(1));
        Assert.That(_fixture.Enrollments.Count, Is.Zero);
    }

    [Test]
    public async Task Nalog_Druge_Uloge_Ne_Dobija_Studentsko_Clanstvo()
    {
        var teacherId = Guid.NewGuid();
        _fixture.Accounts.Seed("prof.petar", teacherId, role: AccountRole.Nastavnik);

        var result = await AsAdmin().HandleAsync(
            _fixture.OfferingId, "firstname,lastname,username\nPetar,Petrović,prof.petar\n");

        Assert.That(result.CreatedMembershipCount, Is.Zero);
        Assert.That(result.ErrorCount, Is.EqualTo(1));
        Assert.That(_fixture.Enrollments.Count, Is.Zero);
    }

    [Test]
    public async Task Ponovljena_Potvrda_Ne_Stvara_Drugi_Nalog_Ni_Drugo_Clanstvo()
    {
        var handler = AsAdmin();

        var first = await handler.HandleAsync(_fixture.OfferingId, StudentImportFixture.SpecExample);
        var second = await handler.HandleAsync(_fixture.OfferingId, StudentImportFixture.SpecExample);

        Assert.That(first.CreatedMembershipCount, Is.EqualTo(17));
        Assert.That(second.CreatedAccountCount, Is.Zero);
        Assert.That(second.CreatedMembershipCount, Is.Zero);
        Assert.That(_fixture.Activations.ActivationRequested, Has.Count.EqualTo(17));
        Assert.That(_fixture.Enrollments.Count, Is.EqualTo(17));
    }

    [Test]
    public async Task Lozinka_Iz_CSV_A_Se_Ignorise_Uz_Upozorenje()
    {
        var result = await AsAdmin().HandleAsync(
            _fixture.OfferingId, "firstname,lastname,username,password\nAna,Anić,ana.anic,tajna123\n");

        Assert.That(result.CreatedAccountCount, Is.EqualTo(1));
        Assert.That(string.Join(" ", result.Warnings), Does.Contain("ignorisana"));
        var account = await _fixture.Accounts.FindByUsernameAsync("ana.anic");
        Assert.That(account!.IsActivated, Is.False);
        Assert.That(_fixture.Activations.ActivationRequested, Is.EqualTo(new[] { account.AccountId }));
    }

    [Test]
    public async Task Aktivacioni_Token_Iz_CSV_A_Se_Ignorise()
    {
        var result = await AsAdmin().HandleAsync(
            _fixture.OfferingId,
            "firstname,lastname,username,token\nAna,Anić,ana.anic,abc-123\n");

        Assert.That(result.CreatedAccountCount, Is.EqualTo(1));
        Assert.That(string.Join(" ", result.Warnings), Does.Contain("token"));
        var account = await _fixture.Accounts.FindByUsernameAsync("ana.anic");
        Assert.That(account!.IsActivated, Is.False);
        Assert.That(_fixture.Activations.ActivationRequested, Is.EqualTo(new[] { account.AccountId }));
    }

    [Test]
    public async Task Tacka_Zarez_I_BOM_Se_Podrzavaju()
    {
        var result = await AsAdmin().HandleAsync(
            _fixture.OfferingId,
            "\uFEFFime;prezime;username;email\nAna;Anić;ana.anic;ana@example.com\n");

        Assert.That(result.CreatedAccountCount, Is.EqualTo(1));
        Assert.That(await _fixture.Accounts.FindByUsernameAsync("ana.anic"), Is.Not.Null);
    }

    [Test]
    public async Task Srpska_Dijakritika_Se_Cuva()
    {
        var result = await AsAdmin().HandleAsync(_fixture.OfferingId,
            "firstname,lastname,username\nĐorđe,Đorđević,đorđe.đorđević\n");
        var account = await _fixture.Accounts.FindByUsernameAsync("đorđe.đorđević");
        Assert.That(result.CreatedAccountCount, Is.EqualTo(1));
        Assert.That(account, Is.Not.Null);
        Assert.That(account!.Username, Is.EqualTo("đorđe.đorđević"));
        Assert.That(account.FirstName, Is.EqualTo("Đorđe"));
        Assert.That(account.LastName, Is.EqualTo("Đorđević"));
    }

    [Test]
    public async Task Ime_Prezime_Iz_Jedne_Kolone_Uz_Upozorenje()
    {
        var result = await AsAdmin().HandleAsync(
            _fixture.OfferingId, "fullname,username\nAna Anić,ana.anic\n");

        Assert.That(result.CreatedAccountCount, Is.EqualTo(1));
        Assert.That(string.Join(" ", result.Warnings), Does.Contain("pretpostavka"));
        var account = await _fixture.Accounts.FindByUsernameAsync("ana.anic");
        Assert.That(account!.FirstName, Is.EqualTo("Ana"));
        Assert.That(account.LastName, Is.EqualTo("Anić"));
    }

    [Test]
    public async Task Potvrda_Obradjuje_Samo_Oznacene_Ispravne_Redove()
    {
        const string csv = "firstname,lastname,username\nAna,Anić,ana.anic\nMarko,Marković,marko.markovic\n";

        // Ред 1 је заглавље; подаци почињу од реда 2.
        var result = await AsAdmin().HandleAsync(_fixture.OfferingId, csv, new[] { 2 });

        Assert.That(result.CreatedMembershipCount, Is.EqualTo(1));
        Assert.That(await _fixture.Accounts.FindByUsernameAsync("marko.markovic"), Is.Null);
        var ana = (await _fixture.Accounts.FindByUsernameAsync("ana.anic"))!;
        Assert.That(await _fixture.Enrollments.FindActiveAsync(ana.AccountId, _fixture.OfferingId), Is.Not.Null);
    }

    [Test]
    public async Task Potvrda_I_Predlog_Govore_Isto_O_Duplikatu_U_Datoteci()
    {
        // Два реда са истим корисничким именом: други је дупликат у целој датотеци.
        const string csv =
            "firstname,lastname,username\nAna,Anić,ana.anic\nAna,Anić,ana.anic\n";

        var preview = await _fixture.PreviewFor(_fixture.AdminId, AccountRoles.Administrator)
            .HandleAsync(_fixture.OfferingId, csv);
        Assert.That(preview.Rows.Count(r => r.Status == RowStatus.Duplikat), Is.EqualTo(1));

        // Избор само другог понављања не уклања дупликат у датотеци.
        var result = await AsAdmin().HandleAsync(_fixture.OfferingId, csv, new[] { 3 });

        Assert.That(result.DuplicateCount, Is.EqualTo(1));
        Assert.That(result.CreatedMembershipCount, Is.Zero);
        Assert.That(await _fixture.Accounts.FindByUsernameAsync("ana.anic"), Is.Null);
    }

    [Test]
    public async Task Oznacen_Red_Greske_Se_Ne_Obradjuje()
    {
        const string csv = "firstname,lastname,username\nAna,Anić,\n";

        var result = await AsAdmin().HandleAsync(_fixture.OfferingId, csv, new[] { 2 });

        Assert.That(result.ErrorCount, Is.EqualTo(1));
        Assert.That(result.CreatedMembershipCount, Is.Zero);
    }
}
