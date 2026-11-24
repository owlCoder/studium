using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Courses;
using Domain.Offerings;
using Domain.Staff;
using Infrastructure.Engagements;
using NUnit.Framework;

namespace Tests.Infrastructure.Engagements;

[TestFixture]
public sealed class InMemoryStaffOfferingsReaderTests
{
    private static readonly Guid Ana = Guid.NewGuid();    // asistentkinja
    private static readonly Guid Marko = Guid.NewGuid();  // nastavnik
    private static readonly Guid Admin = Guid.NewGuid();  // ko uklanja saradnika

    private readonly Course _ers = new(Guid.NewGuid(), "ERS", "Elementi razvoja softvera");
    private readonly Course _oib = new(Guid.NewGuid(), "OIB", "Osnove informacione bezbednosti");
    private readonly Course _bp = new(Guid.NewGuid(), "BP", "Baze podataka");

    private Offering _ersOffering = null!;
    private Offering _oibOffering = null!;
    private Offering _bpOffering = null!;

    [SetUp]
    public void SetUp()
    {
        _ersOffering = new Offering(Guid.NewGuid(), _ers.Id, "2026/27");
        _oibOffering = new Offering(Guid.NewGuid(), _oib.Id, "2026/27");
        _bpOffering = new Offering(Guid.NewGuid(), _bp.Id, "2026/27");
    }

    private InMemoryStaffOfferingsReader Reader(params TeachingAssignment[] assignments) =>
        new(new[] { _ers, _oib, _bp },
            new[] { _ersOffering, _oibOffering, _bpOffering },
            assignments);

    private static TeachingAssignment Assign(Guid userId, Offering offering, Responsibility role) =>
        new(Guid.NewGuid(), offering.Id, userId, role);

    [Test] // Test iz zadatka US-7.2
    public async Task Asistentkinja_NeDobijaPredmetNaKomNijeAngazovana()
    {
        var reader = Reader(
            Assign(Ana, _oibOffering, Responsibility.Assistant),
            Assign(Marko, _ersOffering, Responsibility.ResponsibleTeacher)); // ERS postoji, ali Ana nije na njemu

        var result = await reader.ListActiveForUserAsync(Ana);

        Assert.Multiple(() =>
        {
            Assert.That(result.Select(x => x.OfferingId), Is.EqualTo(new[] { _oibOffering.Id }));
            Assert.That(result.Select(x => x.OfferingId), Does.Not.Contain(_ersOffering.Id));
        });
    }

    [Test]
    public async Task VracaUloguNazivPredmetaIAkademskuGodinu()
    {
        var reader = Reader(
            Assign(Marko, _ersOffering, Responsibility.ResponsibleTeacher),
            Assign(Marko, _oibOffering, Responsibility.Teacher),
            Assign(Ana, _oibOffering, Responsibility.Assistant));

        var marko = await reader.ListActiveForUserAsync(Marko);
        var ana = await reader.ListActiveForUserAsync(Ana);

        var ers = marko.Single(x => x.OfferingId == _ersOffering.Id);
        Assert.Multiple(() =>
        {
            Assert.That(marko, Has.Count.EqualTo(2));
            Assert.That(ers.Role, Is.EqualTo(Responsibility.ResponsibleTeacher));
            Assert.That(ers.CourseName, Is.EqualTo("Elementi razvoja softvera"));
            Assert.That(ers.AcademicYear, Is.EqualTo("2026/27"));
            Assert.That(marko.Single(x => x.OfferingId == _oibOffering.Id).Role, Is.EqualTo(Responsibility.Teacher));
            Assert.That(ana.Single().Role, Is.EqualTo(Responsibility.Assistant));
        });
    }

    [Test]
    public async Task UklonjeniSaradnik_NeDobijaIzvodjenje()
    {
        var removed = Assign(Ana, _bpOffering, Responsibility.Assistant);
        removed.Remove(Admin, new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc));

        var reader = Reader(Assign(Ana, _oibOffering, Responsibility.Assistant), removed);

        var result = await reader.ListActiveForUserAsync(Ana);

        Assert.That(result.Select(x => x.OfferingId), Is.EqualTo(new[] { _oibOffering.Id }));
    }

    [Test]
    public async Task KorisnikBezAngazovanja_DobijaPraznuListu()
    {
        var reader = Reader(Assign(Marko, _ersOffering, Responsibility.Teacher));

        var result = await reader.ListActiveForUserAsync(Ana);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ListActiveForUserAsync_RespectsCancellation()
    {
        var reader = Reader();
        using var source = new CancellationTokenSource();
        source.Cancel();

        Assert.That(
            () => reader.ListActiveForUserAsync(Ana, source.Token),
            Throws.TypeOf<OperationCanceledException>());
    }
}
