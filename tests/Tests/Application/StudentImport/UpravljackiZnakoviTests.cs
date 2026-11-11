using Application.StudentImport;
using NUnit.Framework;

namespace Tests.Application.StudentImport;

/// <summary>Одбија управљачке знакове у пољима пре уписа.</summary>
[TestFixture]
public sealed class UpravljackiZnakoviTests
{
    private static async Task<ImportRowResult> Preview(string csv)
    {
        var fixture = new StudentImportFixture();
        var preview = await fixture.PreviewFor(fixture.AdminId, AccountRoles.Administrator)
            .HandleAsync(fixture.OfferingId, csv);

        return preview.Rows[0];
    }

    [Test]
    public async Task Nulti_Znak_U_Korisnickom_Imenu_Je_Greska()
        => Assert.That(
            (await Preview("firstname,lastname,username\nAna,Anić,ana\u0000anic\n")).Status,
            Is.EqualTo(RowStatus.Greska));

    [Test]
    public async Task Upravljacki_Znak_U_Imenu_Je_Greska()
        => Assert.That(
            (await Preview("firstname,lastname,username\nAna\u0007,Anić,ana.anic\n")).Status,
            Is.EqualTo(RowStatus.Greska));

    [Test]
    public async Task Tab_Unutar_Imena_Je_Greska_A_Na_Kraju_Nije()
    {
        Assert.That(
            (await Preview("firstname,lastname,username\nAn\ta,Anić,ana.anic\n")).Status,
            Is.EqualTo(RowStatus.Greska));

        // Размаци и табови око поља су само околни празан простор, не део податка.
        Assert.That(
            (await Preview("firstname,lastname,username\nAna,Anić,\tana.anic\t\n")).Status,
            Is.EqualTo(RowStatus.Ispravan));
    }

    [Test]
    public async Task Kontrolni_Znak_U_EPosti_Je_Greska()
        => Assert.That(
            (await Preview("firstname,lastname,username,email\nAna,Anić,ana.anic,ana\u0000@example.com\n")).Status,
            Is.EqualTo(RowStatus.Greska));
}
