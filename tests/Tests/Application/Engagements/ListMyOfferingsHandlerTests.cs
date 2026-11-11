using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Engagements;
using Application.Enrollments;
using Domain.Staff;
using Moq;
using NUnit.Framework;

namespace Tests.Application.Engagements;

[TestFixture]
public sealed class ListMyOfferingsHandlerTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _ers = Guid.NewGuid();
    private readonly Guid _oib = Guid.NewGuid();
    private readonly Guid _bp = Guid.NewGuid();

    private Mock<IStaffOfferingsReader> _staff = null!;
    private Mock<IStudentOfferingsReader> _student = null!;

    [SetUp]
    public void SetUp()
    {
        _staff = new Mock<IStaffOfferingsReader>();
        _student = new Mock<IStudentOfferingsReader>();
        SetupStaff();
        SetupStudent();
    }

    private void SetupStaff(params TeachingOfferingDto[] rows) =>
        _staff.Setup(x => x.ListActiveForUserAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

    private void SetupStudent(params EnrolledOfferingDto[] rows) =>
        _student.Setup(x => x.ListActiveForStudentAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

    private ListMyOfferingsHandler Handler() => new(_staff.Object, _student.Object);

    [Test]
    public async Task StudentISaradnik_DobijaObeListe_BezDuplikata()
    {
        // Asistent na OIB, a student na ERS i BP. Na OIB je upisan i kao student.
        SetupStaff(new TeachingOfferingDto(_oib, "Osnove informacione bezbednosti", "2026/27", Responsibility.Assistant));
        SetupStudent(
            new EnrolledOfferingDto(_ers, "Elementi razvoja softvera", "2026/27"),
            new EnrolledOfferingDto(_bp, "Baze podataka", "2026/27"),
            new EnrolledOfferingDto(_ers, "Elementi razvoja softvera", "2026/27"),
            new EnrolledOfferingDto(_oib, "Osnove informacione bezbednosti", "2026/27"));

        var result = await Handler().HandleAsync(_userId);

        var all = result.Teaching.Select(x => x.OfferingId)
            .Concat(result.Enrolled.Select(x => x.OfferingId))
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(result.Teaching.Select(x => x.OfferingId), Is.EqualTo(new[] { _oib }));
            Assert.That(result.Enrolled.Select(x => x.OfferingId), Is.EquivalentTo(new[] { _ers, _bp }));
            Assert.That(all, Is.Unique);
        });
    }

    [Test]
    public async Task ViseAngazovanjaNaIstomIzvodjenju_JedanRedSaNajvisomUlogom()
    {
        SetupStaff(
            new TeachingOfferingDto(_ers, "Elementi razvoja softvera", "2026/27", Responsibility.Assistant),
            new TeachingOfferingDto(_ers, "Elementi razvoja softvera", "2026/27", Responsibility.ResponsibleTeacher),
            new TeachingOfferingDto(_ers, "Elementi razvoja softvera", "2026/27", Responsibility.Teacher));

        var result = await Handler().HandleAsync(_userId);

        Assert.Multiple(() =>
        {
            Assert.That(result.Teaching, Has.Count.EqualTo(1));
            Assert.That(result.Teaching[0].Role, Is.EqualTo(Responsibility.ResponsibleTeacher));
            Assert.That(result.Teaching[0].RoleName, Is.EqualTo("ResponsibleTeacher"));
        });
    }

    [Test]
    public async Task SortiraPoGodiniPaPoNazivu()
    {
        SetupStaff(
            new TeachingOfferingDto(_oib, "Osnove informacione bezbednosti", "2026/27", Responsibility.Teacher),
            new TeachingOfferingDto(_bp, "Baze podataka", "2025/26", Responsibility.Teacher),
            new TeachingOfferingDto(_ers, "Elementi razvoja softvera", "2026/27", Responsibility.Teacher));

        var result = await Handler().HandleAsync(_userId);

        Assert.That(result.Teaching.Select(x => x.CourseName), Is.EqualTo(new[]
        {
            "Elementi razvoja softvera",
            "Osnove informacione bezbednosti",
            "Baze podataka"
        }));
    }

    [Test]
    public async Task KorisnikBezAngazovanjaIUpisa_DobijaPrazneListe()
    {
        var result = await Handler().HandleAsync(_userId);

        Assert.Multiple(() =>
        {
            Assert.That(result.Teaching, Is.Empty);
            Assert.That(result.Enrolled, Is.Empty);
        });
    }

    [Test]
    public void OdbijaPrazanIdKorisnika()
    {
        Assert.ThrowsAsync<ArgumentException>(() => Handler().HandleAsync(Guid.Empty));

        _staff.Verify(x => x.ListActiveForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _student.Verify(x => x.ListActiveForStudentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
