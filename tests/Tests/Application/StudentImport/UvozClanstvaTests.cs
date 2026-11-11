using Application.StudentImport;
using NUnit.Framework;

namespace Tests.Application.StudentImport;

/// <summary>Увоз чланства користи постојећи налог без обавезног имена и презимена.</summary>
[TestFixture]
public sealed class UvozClanstvaTests
{
    private const string OnlyUsernames = "username\nana.anic\n";

    /// <summary>Корисничко име постојећег налога је довољно за упис чланства.</summary>
    [Test]
    public async Task Spisak_Samo_Korisnickih_Imena_Pravi_Clanstva()
    {
        var fixture = new StudentImportFixture();
        var existingId = Guid.NewGuid();
        fixture.Accounts.Seed("ana.anic", existingId);

        var preview = await fixture.PreviewFor(fixture.AdminId, AccountRoles.Administrator)
            .HandleAsync(fixture.OfferingId, OnlyUsernames);

        Assert.That(preview.Rows.Single().Status, Is.EqualTo(RowStatus.Ispravan));

        var result = await fixture.ConfirmFor(fixture.AdminId, AccountRoles.Administrator)
            .HandleAsync(fixture.OfferingId, OnlyUsernames);

        Assert.That(result.CreatedMembershipCount, Is.EqualTo(1));
        Assert.That(result.CreatedAccountCount, Is.Zero);
        Assert.That(await fixture.Enrollments.FindActiveAsync(existingId, fixture.OfferingId), Is.Not.Null);
    }

    /// <summary>Мешовита датотека: познат налог пролази, непознат без имена пада.</summary>
    [Test]
    public async Task Mesovita_Datoteka()
    {
        var fixture = new StudentImportFixture();
        fixture.Accounts.Seed("postoji.vec", Guid.NewGuid());

        const string csv = "firstname,lastname,username\n" +
                           "Bez,Imena,postoji.vec\n" +   // ред 2 — име није потребно
                           ",,novi.student\n" +          // ред 3 — нема имена → Грешка
                           "Novi,Student,drugi.novi\n";  // ред 4 — има име → Исправан

        var preview = await fixture.PreviewFor(fixture.AdminId, AccountRoles.Administrator)
            .HandleAsync(fixture.OfferingId, csv);

        Assert.That(preview.Rows[0].Status, Is.EqualTo(RowStatus.Ispravan));
        Assert.That(preview.Rows[1].Status, Is.EqualTo(RowStatus.Greska));
        Assert.That(preview.Rows[2].Status, Is.EqualTo(RowStatus.Ispravan));
    }

    /// <summary>Увоз не преписује личне податке постојећег налога.</summary>
    [Test]
    public async Task Ime_Postojeceg_Naloga_Se_Ne_Prepisuje()
    {
        var fixture = new StudentImportFixture();
        var existingId = Guid.NewGuid();
        fixture.Accounts.Seed("ana.anic", existingId, firstName: "Ana", lastName: "Anić");

        var result = await fixture.ConfirmFor(fixture.AdminId, AccountRoles.Administrator)
            .HandleAsync(fixture.OfferingId,
                "firstname,lastname,username\nSasvim,Drugo,ana.anic\n");

        var account = (await fixture.Accounts.FindByUsernameAsync("ana.anic"))!;
        Assert.That(result.CreatedAccountCount, Is.Zero);
        Assert.That(fixture.Activations.ActivationRequested, Is.Empty);
        Assert.That(account.AccountId, Is.EqualTo(existingId));
        Assert.That(account.FirstName, Is.EqualTo("Ana"));
        Assert.That(account.LastName, Is.EqualTo("Anić"));
        Assert.That(result.CreatedMembershipCount, Is.EqualTo(1));
        Assert.That((await fixture.Enrollments.FindActiveAsync(account.AccountId, fixture.OfferingId))!.AccountId,
            Is.EqualTo(account.AccountId));
    }
}
