using System.Text;
using Application.StudentImport;
using NUnit.Framework;

namespace Tests.Application.StudentImport;

/// <summary>Поновљен увоз не ствара дупликате ни за канонски еквивалентне записе имена.</summary>
[TestFixture]
public sealed class PonovljeniUvozTests
{
    private static readonly string Nfc = "đorđe.đorđević".Normalize(NormalizationForm.FormC);
    private static readonly string Nfd = "đorđe.đorđević".Normalize(NormalizationForm.FormD);

    private static string Csv(string username)
        => $"firstname,lastname,username\nĐorđe,Đorđević,{username}\n";

    [Test]
    public async Task Dva_Uvoza_Razlicitog_Unicode_Zapisa_Ne_Stvaraju_Drugi_Nalog()
    {
        var fixture = new StudentImportFixture();
        var handler = fixture.ConfirmFor(fixture.AdminId, AccountRoles.Administrator);

        var first = await handler.HandleAsync(fixture.OfferingId, Csv(Nfd));
        var second = await handler.HandleAsync(fixture.OfferingId, Csv(Nfc));

        Assert.That(first.CreatedAccountCount, Is.EqualTo(1));
        Assert.That(second.CreatedAccountCount, Is.Zero);
        Assert.That(second.CreatedMembershipCount, Is.Zero);
        Assert.That(second.DuplicateCount, Is.EqualTo(1));
        Assert.That(fixture.Enrollments.Count, Is.EqualTo(1));
        Assert.That(fixture.Activations.ActivationRequested, Has.Count.EqualTo(1));
    }

    /// <summary>Нормализација корисничког имена чува величину слова.</summary>
    [Test]
    public void Canonicalni_Oblik_Cuva_Velika_Slova()
    {
        Assert.That(UsernameComparison.Canonical("Ana.Petrovic"), Is.EqualTo("Ana.Petrovic"));
        Assert.That(UsernameComparison.Canonical(Nfd), Is.EqualTo(Nfc));
    }

    [Test]
    public async Task Uloga_Se_Poredi_Bez_Obzira_Na_Velicinu_Slova()
    {
        var fixture = new StudentImportFixture();

        var preview = await fixture.PreviewFor(fixture.AdminId, "administrator")
            .HandleAsync(fixture.OfferingId, Csv("ana.anic"));

        Assert.That(preview.Rows.Single().Status, Is.EqualTo(RowStatus.Ispravan));
    }

    /// <summary>Адаптер одбија дупликатно корисничко име без преписивања налога.</summary>
    [Test]
    public async Task Dva_Naloga_Sa_Istim_Imenom_Su_Nemoguci()
    {
        var fixture = new StudentImportFixture();
        await fixture.Accounts.AddAsync(new NewUserAccount(
            "Ana", "Anić", "ana.anic",
            AccountRole.Student, AccountState.Aktivan, IsActivated: false,
            Email: null, IsEmailConfirmed: false));

        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await fixture.Accounts.AddAsync(new NewUserAccount(
                "Druga", "Osoba", "ANA.ANIC",
                AccountRole.Student, AccountState.Aktivan, IsActivated: false,
                Email: null, IsEmailConfirmed: false)));
    }
}

[TestFixture]
public sealed class VelicinaSlovaTests
{
    /// <summary>Увоз чува величину слова; порт пореди корисничка имена.</summary>
    [Test]
    public async Task Velicina_Slova_Se_Predaje_Neizmenjena()
    {
        var fixture = new StudentImportFixture();

        await fixture.ConfirmFor(fixture.AdminId, AccountRoles.Administrator)
            .HandleAsync(fixture.OfferingId,
                "firstname,lastname,username\nAna,Petrović,Ana.Petrovic\n");

        // Корисничко име задржава величину слова из датотеке.
        var canonical = await fixture.Accounts.FindByUsernameAsync("Ana.Petrovic");
        var lowercased = await fixture.Accounts.FindByUsernameAsync("ana.petrovic");

        Assert.That(canonical, Is.Not.Null);
        Assert.That(canonical!.Username, Is.EqualTo("Ana.Petrovic"));
        Assert.That(lowercased, Is.Not.Null);
        Assert.That(lowercased!.AccountId, Is.EqualTo(canonical.AccountId));
    }

    /// <summary>Дијакритички знаци у корисничком имену остају сачувани.</summary>
    [Test]
    public async Task Dijakritika_Je_Ispravna()
    {
        var fixture = new StudentImportFixture();

        var preview = await fixture.PreviewFor(fixture.AdminId, AccountRoles.Administrator)
            .HandleAsync(fixture.OfferingId,
                "firstname,lastname,username\nĐorđe,Đorđević,đorđe.đorđević\n");

        Assert.That(preview.Rows.Single().Status, Is.EqualTo(RowStatus.Ispravan));
        Assert.That(preview.Rows.Single().Reason, Is.Empty);
    }
}
