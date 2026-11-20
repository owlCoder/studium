using Application.StudentImport;
using NUnit.Framework;

namespace Tests.Application.StudentImport;

// Прекинуто чланство се поново активира; активно чланство је дупликат.

[TestFixture]
public sealed class AktivnoClanstvoTests
{
    private const string JedanRed =
        "firstname,lastname,username\n" +
        "Ana,Anić,ana.anic\n";

    [Test]
    public async Task Prekinuto_Clanstvo_Dozvoljava_Ponovnu_Aktivaciju()
    {
        var fixture = new StudentImportFixture();
        var accountId = Guid.NewGuid();
        fixture.Accounts.Seed("ana.anic", accountId);
        fixture.Enrollments.Seed(accountId, fixture.OfferingId, EnrollmentState.Prekinuto);

        var preview = await fixture.PreviewFor(fixture.AdminId, AccountRoles.Administrator)
            .HandleAsync(fixture.OfferingId, JedanRed);

        // Прекинуто чланство није активно, па ред није дупликат.
        Assert.That(preview.Rows.Single().Status, Is.EqualTo(RowStatus.Ispravan));
    }

    [Test]
    public async Task Potvrda_Aktivira_Isti_Zapis_Clanstva()
    {
        var fixture = new StudentImportFixture();
        var accountId = Guid.NewGuid();
        fixture.Accounts.Seed("ana.anic", accountId);
        fixture.Enrollments.Seed(accountId, fixture.OfferingId, EnrollmentState.Prekinuto);

        var before = fixture.Enrollments.All.Single();
        var result = await fixture.ConfirmFor(fixture.AdminId, AccountRoles.Administrator)
            .HandleAsync(fixture.OfferingId, JedanRed);

        Assert.That(result.CreatedMembershipCount, Is.EqualTo(1));
        Assert.That(fixture.Enrollments.All.Count(e => e.AccountId == accountId), Is.EqualTo(1));
        Assert.That(fixture.Enrollments.All.Count(e => e.State == EnrollmentState.Prekinuto), Is.Zero);
        Assert.That(fixture.Enrollments.All.Count(e => e.State == EnrollmentState.Aktivno), Is.EqualTo(1));

        var aktivno = await fixture.Enrollments.FindActiveAsync(accountId, fixture.OfferingId);
        Assert.That(aktivno, Is.Not.Null);
        Assert.That(aktivno!.EnrollmentId, Is.EqualTo(before.EnrollmentId));
        Assert.That(aktivno.EnrolledAt, Is.EqualTo(before.EnrolledAt));
        Assert.That(aktivno.State, Is.EqualTo(EnrollmentState.Aktivno));
        var repeated = await fixture.ConfirmFor(fixture.AdminId, AccountRoles.Administrator)
            .HandleAsync(fixture.OfferingId, JedanRed);
        Assert.That(repeated.CreatedMembershipCount, Is.Zero);
        Assert.That(repeated.DuplicateCount, Is.EqualTo(1));
        Assert.That(fixture.Enrollments.All.Single().EnrollmentId, Is.EqualTo(before.EnrollmentId));
    }

    [Test]
    public async Task Novo_Clanstvo_Se_Upisuje_Kao_Aktivno()
    {
        var fixture = new StudentImportFixture();

        await fixture.ConfirmFor(fixture.AdminId, AccountRoles.Administrator)
            .HandleAsync(fixture.OfferingId, JedanRed);

        // Ново чланство је активно.
        var nalog = await fixture.Accounts.FindByUsernameAsync("ana.anic");
        Assert.That(nalog, Is.Not.Null);

        var upisano = await fixture.Enrollments.FindActiveAsync(
            nalog!.AccountId, fixture.OfferingId);

        Assert.That(upisano, Is.Not.Null);
        Assert.That(upisano!.State, Is.EqualTo(EnrollmentState.Aktivno));
        Assert.That(upisano.EnrolledAt, Is.EqualTo(StudentImportFixture.Now));
    }
}
