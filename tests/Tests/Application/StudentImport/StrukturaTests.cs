using Application.StudentImport;
using NUnit.Framework;

namespace Tests.Application.StudentImport;

/// <summary>Проверава замену портова и додавање правила без измене постојећих класа.</summary>
[TestFixture]
public sealed class StrukturaTests
{
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
