using Domain.Courses;
using Domain.Enrollments;
using Domain.Offerings;
using Infrastructure.Enrollments;
using NUnit.Framework;

namespace Tests.Infrastructure.Enrollments;

[TestFixture]
public sealed class InMemoryStudentOfferingsReaderTests
{
    private static readonly Guid Student = Guid.NewGuid();
    private static readonly Guid OtherStudent = Guid.NewGuid();

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

    private InMemoryStudentOfferingsReader Reader(params Membership[] memberships) =>
        new(new[] { _ers, _oib, _bp },
            new[] { _ersOffering, _oibOffering, _bpOffering },
            memberships);

    private static Membership Enroll(Guid studentId, Offering offering,
                                     MembershipStatus status = MembershipStatus.Active) =>
        new(Guid.NewGuid(), studentId, offering.Id, status);

    [Test] 
    public async Task StudentSaDvaPredmeta_DobijaOba_ANeDobijaTreci()
    {
        var reader = Reader(
            Enroll(Student, _ersOffering),
            Enroll(Student, _oibOffering),
            Enroll(OtherStudent, _bpOffering)); 

        var result = await reader.ListActiveForStudentAsync(Student);

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result.Select(x => x.OfferingId),
                Is.EquivalentTo(new[] { _ersOffering.Id, _oibOffering.Id }));
            Assert.That(result.Select(x => x.OfferingId), Does.Not.Contain(_bpOffering.Id));
        });
    }

    [Test]
    public async Task VracaNazivPredmetaIAkademskuGodinu()
    {
        var reader = Reader(Enroll(Student, _ersOffering));

        var result = await reader.ListActiveForStudentAsync(Student);

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].CourseName, Is.EqualTo("Elementi razvoja softvera"));
            Assert.That(result[0].AcademicYear, Is.EqualTo("2026/27"));
        });
    }

    [Test]
    public async Task PrekinutoClanstvo_SeIzostavlja()
    {
        var reader = Reader(
            Enroll(Student, _ersOffering),
            Enroll(Student, _oibOffering, MembershipStatus.Terminated));

        var result = await reader.ListActiveForStudentAsync(Student);

        Assert.That(result.Select(x => x.OfferingId), Is.EqualTo(new[] { _ersOffering.Id }));
    }

    [Test]
    public async Task ClanstvoPrekinutoMetodomTerminate_SeIzostavlja()
    {
        var membership = Enroll(Student, _ersOffering);
        membership.Terminate();

        var result = await Reader(membership).ListActiveForStudentAsync(Student);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task DvostrukiUpisNaIstoIzvodjenje_VracaSeJednom()
    {
        var reader = Reader(Enroll(Student, _ersOffering), Enroll(Student, _ersOffering));

        var result = await reader.ListActiveForStudentAsync(Student);

        Assert.That(result, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task StudentBezClanstava_DobijaPraznuListu()
    {
        var reader = Reader(Enroll(OtherStudent, _ersOffering));

        var result = await reader.ListActiveForStudentAsync(Student);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ListActiveForStudentAsync_RespectsCancellation()
    {
        var reader = Reader();
        using var source = new CancellationTokenSource();
        source.Cancel();

        Assert.That(
            () => reader.ListActiveForStudentAsync(Student, source.Token),
            Throws.TypeOf<OperationCanceledException>());
    }
}
