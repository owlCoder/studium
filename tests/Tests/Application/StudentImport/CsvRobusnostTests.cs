using Application.StudentImport;
using NUnit.Framework;

namespace Tests.Application.StudentImport;

/// <summary>Провере CSV наводника, раздељивача, прелома редова и неисправних записа.</summary>
[TestFixture]
public sealed class CsvRobusnostTests
{
    private static ParsedStudentRow ParseSingle(string csv)
        => new StudentListCsvParser().Parse(csv).Rows.Single();

    [Test]
    public void Razdeljivac_Unutar_Navodnika_Ne_Razdvaja_Polje()
    {
        var row = ParseSingle("firstname,lastname,username\n\"Anić, mlađi\",Ana,ana.anic\n");

        // Зарез у пољу под наводницима није раздељивач.
        Assert.That(row.FirstName, Is.EqualTo("Anić, mlađi"));
        Assert.That(row.Username, Is.EqualTo("ana.anic"));
    }

    [Test]
    public void Dupli_Navodnik_Unutar_Polja_Ostaje_Jedan_Navodnik()
    {
        var row = ParseSingle("firstname,lastname,username\nAna,\"Anić \"\"mlađi\"\"\",ana.anic\n");

        Assert.That(row.LastName, Is.EqualTo("Anić \"mlađi\""));
        Assert.That(row.Username, Is.EqualTo("ana.anic"));
    }

    /// <summary>Прелом реда под наводницима не ствара додатни CSV запис.</summary>
    [Test]
    public void Prelom_Reda_Unutar_Navodnika_Ostaje_Jedan_Red()
    {
        var parsed = new StudentListCsvParser()
            .Parse("firstname,lastname,username\nAna,\"Anić\nMarković\",ana.anic\n");

        Assert.That(parsed.Rows, Has.Count.EqualTo(1));
        Assert.That(parsed.Rows[0].LastName, Is.EqualTo("Anić\nMarković"));
        Assert.That(parsed.Rows[0].RowNumber, Is.EqualTo(2));
    }

    [Test]
    public void Nezatvoren_Navodnik_Odbija_Celu_Datoteku()
    {
        var parser = new StudentListCsvParser();

        Assert.That(
            () => parser.Parse("firstname,lastname,username\nAna,\"Anić,ana.anic\n"),
            Throws.TypeOf<ImportFileRejectedException>().With.Message.Contains("navodnik"));
    }

    [Test]
    public async Task Odbijena_Datoteka_Ne_Menja_Nijedan_Zapis()
    {
        var fixture = new StudentImportFixture();

        Assert.ThrowsAsync<ImportFileRejectedException>(async () =>
            await fixture.ConfirmFor(fixture.AdminId, AccountRoles.Administrator)
                .HandleAsync(
                    fixture.OfferingId,
                    "firstname,lastname,username\nMarko,Markovic,marko.markovic\nAna,\"Anić,ana.anic\n"));

        Assert.That(fixture.Activations.ActivationRequested, Is.Empty);
        Assert.That(fixture.Enrollments.Count, Is.Zero);
        Assert.That(await fixture.Accounts.FindByUsernameAsync("ana.anic"), Is.Null);
        Assert.That(await fixture.Accounts.FindByUsernameAsync("marko.markovic"), Is.Null);
    }

    /// <summary>Тачка-зарез у пољу под наводницима није раздељивач.</summary>
    [Test]
    public void Tacka_Zarez_Sa_Navodnicima()
    {
        var row = ParseSingle("firstname;lastname;username\n\"Anić; mlađi\";Ana;ana.anic\n");

        Assert.That(row.FirstName, Is.EqualTo("Anić; mlađi"));
        Assert.That(row.Username, Is.EqualTo("ana.anic"));
    }

    [Test]
    public void Red_Sa_Manje_Kolona_Nije_Greska_Formata()
    {
        // Недостајуће поље је празно; о томе одлучује правило, не читач.
        var row = ParseSingle("firstname,lastname,username\nAna\n");

        Assert.That(row.Username, Is.Empty);
        Assert.That(row.LastName, Is.Empty);
    }

    [Test]
    public void Red_Sa_Vise_Kolona_Se_Cita_Bez_Pucanja()
    {
        var row = ParseSingle("firstname,lastname,username\nAna,Anić,ana.anic,višak,još\n");

        Assert.That(row.Username, Is.EqualTo("ana.anic"));
    }

    [Test]
    public void Vodeci_Prazan_Red_Ne_Kvari_Prepoznavanje_Tacka_Zareza()
    {
        var parsed = new StudentListCsvParser()
            .Parse("\n  \nfirstname;lastname;username\nAna;Anić;ana.anic\n");

        Assert.That(parsed.Rows.Single().RowNumber, Is.EqualTo(4));
        Assert.That(parsed.Rows.Single().Username, Is.EqualTo("ana.anic"));
    }
}
