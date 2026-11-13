using Application.Contributions;
using Domain.Models;
using Infrastructure.Contributions;
using NUnit.Framework;

namespace Tests;

[TestFixture]
public class ContributionQueriesTests
{
    private InMemoryContributionReader _store = null!;
    private ContributionQueries _queries = null!;

    [SetUp]
    public void SetUp()
    {
        _store = new InMemoryContributionReader();
        _queries = new ContributionQueries(_store);
    }

    private static Contribution Make(int id, int author, string description = "Opis") =>
        new(id, 100 + id, author, 1, description, Array.Empty<Evidence>());

    [Test]
    public void List_Student_VidiSamoSvoje()
    {
        _store.Add(Make(1, 10));
        _store.Add(Make(2, 20));

        var result = _queries.List(new Viewer(10, ViewerRole.Student));

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].AuthorId, Is.EqualTo(10));
    }

    [Test]
    public void List_Nastavnik_VidiSve()
    {
        _store.Add(Make(1, 10));
        _store.Add(Make(2, 20));

        var result = _queries.List(new Viewer(99, ViewerRole.Teacher));

        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public void List_StudentBezDoprinosa_VracaPraznuListu()
    {
        _store.Add(Make(1, 20));

        var result = _queries.List(new Viewer(10, ViewerRole.Student));

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void TryGetDetails_SopstveniDoprinos_VracaDetalje()
    {
        _store.Add(Make(1, 10, "Prvi opis"));

        var ok = _queries.TryGetDetails(new Viewer(10, ViewerRole.Student), 1, out var dto, out var error);

        Assert.That(ok, Is.True);
        Assert.That(error, Is.Empty);
        Assert.That(dto!.CurrentRevision.Description, Is.EqualTo("Prvi opis"));
        Assert.That(dto.Status, Is.EqualTo("Podnet"));
    }

    [Test]
    public void TryGetDetails_VracaIdZaduzenja()
    {
        _store.Add(Make(1, 10));

        _queries.TryGetDetails(new Viewer(10, ViewerRole.Student), 1, out var dto, out _);

        Assert.That(dto!.IdTask, Is.EqualTo(101));
    }

    [Test]
    public void TryGetDetails_TudjiDoprinos_Odbijeno()
    {
        _store.Add(Make(1, 10));

        var ok = _queries.TryGetDetails(new Viewer(20, ViewerRole.Student), 1, out var dto, out var error);

        Assert.That(ok, Is.False);
        Assert.That(dto, Is.Null);
        Assert.That(error, Is.Not.Empty);
    }

    [Test]
    public void TryGetDetails_NepostojeciId_Odbijeno()
    {
        var ok = _queries.TryGetDetails(new Viewer(10, ViewerRole.Student), 123, out var dto, out _);

        Assert.That(ok, Is.False);
        Assert.That(dto, Is.Null);
    }

    [Test]
    public void TryGetDetails_NepostojeciITudji_ImajuIstuPoruku()
    {
        _store.Add(Make(1, 10));
        var student = new Viewer(20, ViewerRole.Student);

        _queries.TryGetDetails(student, 1, out _, out var tudji);
        _queries.TryGetDetails(student, 999, out _, out var nepostojeci);

        Assert.That(tudji, Is.EqualTo(nepostojeci));
    }

    [Test]
    public void TryGetDetails_Nastavnik_MozeDaOtvoriTudjiDoprinos()
    {
        _store.Add(Make(1, 10));

        var ok = _queries.TryGetDetails(new Viewer(99, ViewerRole.Teacher), 1, out var dto, out _);

        Assert.That(ok, Is.True);
        Assert.That(dto, Is.Not.Null);
    }

    [Test]
    public void TryGetDetails_PosleDopune_CuvaRanijuVerziju()
    {
        var c = Make(1, 10, "Prvi opis");
        c.ReturnForSupplement("Dopuni opis", out _);
        c.Supplement(10, "Drugi opis", Array.Empty<Evidence>(), out _);
        _store.Add(c);

        _queries.TryGetDetails(new Viewer(10, ViewerRole.Student), 1, out var dto, out _);

        Assert.That(dto!.History, Has.Count.EqualTo(2));
        Assert.That(dto.History[0].Description, Is.EqualTo("Prvi opis"));
        Assert.That(dto.History[0].ReviewerNote, Is.EqualTo("Dopuni opis"));
        Assert.That(dto.CurrentRevision.Description, Is.EqualTo("Drugi opis"));
        Assert.That(dto.Status, Is.EqualTo("Podnet"));
    }

    [Test]
    public void TryGetDetails_VracenNaDopunu_PrikazujeStanje()
    {
        var c = Make(1, 10);
        c.ReturnForSupplement("Dopuni", out _);
        _store.Add(c);

        _queries.TryGetDetails(new Viewer(10, ViewerRole.Student), 1, out var dto, out _);

        Assert.That(dto!.Status, Is.EqualTo("Vraćen na dopunu"));
    }

    [Test]
    public void TryGetDetails_Dokaz_PrikazujeVezuIliDatoteku()
    {
        var c = new Contribution(1, 101, 10, 1, "Opis", new[]
        {
            new Evidence { Id = 1, Link = "https://primer.rs" },
            new Evidence { Id = 2, FilePath = "dokazi/a.pdf" }
        });
        _store.Add(c);

        _queries.TryGetDetails(new Viewer(10, ViewerRole.Student), 1, out var dto, out _);

        var evidence = dto!.CurrentRevision.Evidence;
        Assert.That(evidence[0].Kind, Is.EqualTo("Veza"));
        Assert.That(evidence[0].Value, Is.EqualTo("https://primer.rs"));
        Assert.That(evidence[1].Kind, Is.EqualTo("Datoteka"));
        Assert.That(evidence[1].Value, Is.EqualTo("dokazi/a.pdf"));
    }
}
