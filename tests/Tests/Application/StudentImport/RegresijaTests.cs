using Application.StudentImport;
using NUnit.Framework;

namespace Tests.Application.StudentImport;

/// <summary>Провере редоследа правила, поновне потврде и почетног стања налога.</summary>
[TestFixture]
public sealed class RegresijaTests
{
    private StudentImportFixture _fixture = null!;

    [SetUp]
    public void SetUp() => _fixture = new StudentImportFixture();

    private PreviewStudentImportHandler AsAdmin()
        => _fixture.PreviewFor(_fixture.AdminId, AccountRoles.Administrator);

    [Test]
    public async Task Neispravan_Red_Ne_Zauzima_Korisnicko_Ime()
    {
        // Ред 2 има неисправну адресу; ред 3 има исто корисничко име.
        const string csv =
            "firstname,lastname,username,email\n" +
            "Ana,Anić,ana.anic,ana@\n" +
            "Ana,Anić,ana.anic,ana@example.com\n";

        var preview = await AsAdmin().HandleAsync(_fixture.OfferingId, csv);

        Assert.That(preview.Rows[0].Status, Is.EqualTo(RowStatus.Greska));
        Assert.That(preview.Rows[1].Status, Is.EqualTo(RowStatus.Ispravan));
        Assert.That(preview.CorrectCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Brojevi_Redova_Odgovaraju_Fizickim_Redovima_Datoteke()
    {
        // Заглавље = 1, празан ред = 3, Марко = 4.
        const string csv =
            "firstname,lastname,username\n" +
            "Ana,Anić,ana.anic\n" +
            "\n" +
            "Marko,Marković,marko.markovic\n";

        var preview = await AsAdmin().HandleAsync(_fixture.OfferingId, csv);

        Assert.That(preview.TotalCount, Is.EqualTo(2));
        Assert.That(preview.Rows.Select(r => r.RowNumber), Is.EqualTo(new[] { 2, 4 }));
    }

    [Test]
    public async Task Novi_Nalog_Nije_Spreman_Za_Prijavu()
    {
        await _fixture.ConfirmFor(_fixture.AdminId, AccountRoles.Administrator)
            .HandleAsync(_fixture.OfferingId, "firstname,lastname,username\nAna,Anić,ana.anic\n");

        var account = await _fixture.Accounts.FindByUsernameAsync("ana.anic");

        // Нови налог има захтев за прву активацију, али још није активиран.
        Assert.That(account, Is.Not.Null);
        Assert.That(account!.IsActivated, Is.False);
        Assert.That(_fixture.Activations.ActivationRequested, Is.EqualTo(new[] { account.AccountId }));
    }

    [Test]
    public async Task Potvrda_Ne_Upisuje_Drugo_Clanstvo_Ako_Je_Do_Njega_Doslo_Posle_Predloga()
    {
        var accountId = Guid.NewGuid();
        _fixture.Accounts.Seed("ana.anic", accountId);

        const string csv = "firstname,lastname,username\nAna,Anić,ana.anic\n";
        var preview = await AsAdmin().HandleAsync(_fixture.OfferingId, csv);
        Assert.That(preview.CorrectCount, Is.EqualTo(1));

        // Између предлога и потврде Ана је већ уписана на исто извођење.
        _fixture.Enrollments.Seed(accountId, _fixture.OfferingId);

        var result = await _fixture.ConfirmFor(_fixture.AdminId, AccountRoles.Administrator)
            .HandleAsync(_fixture.OfferingId, csv);

        Assert.That(result.CreatedMembershipCount, Is.Zero);
        Assert.That(result.DuplicateCount, Is.EqualTo(1));
        Assert.That(_fixture.Enrollments.Count, Is.EqualTo(1));
    }
}

[TestFixture]
public sealed class GranicaOdgovornostiTests
{

    /// <summary>Увоз проверава постојање извођења, без додатних забрана по стању или архиви.</summary>
    [TestCase(OfferingState.UPripremi, false)]
    [TestCase(OfferingState.Aktivno, false)]
    [TestCase(OfferingState.Zavrseno, true)]
    public async Task Uvoz_Ne_Dodaje_Politiku_Stanja_Izvodjenja(OfferingState state, bool archived)
    {
        var fixture = new StudentImportFixture();
        var offeringId = Guid.NewGuid();
        fixture.Offerings.Seed(new OfferingSnapshot(offeringId, fixture.CourseId,
            fixture.AcademicYearId, state, archived));
        const string csv = "firstname,lastname,username\nAna,Anic,ana.anic\n";
        var preview = await fixture.PreviewFor(fixture.AdminId, AccountRoles.Administrator).HandleAsync(offeringId, csv);
        var result = await fixture.ConfirmFor(fixture.AdminId, AccountRoles.Administrator).HandleAsync(offeringId, csv);
        Assert.That(preview.CorrectCount, Is.EqualTo(1));
        Assert.That(result.CreatedMembershipCount, Is.EqualTo(1));
        Assert.That(fixture.Enrollments.All.Single().OfferingId, Is.EqualTo(offeringId));
    }
}
