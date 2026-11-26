using Application.Activities;
using Application.Courses;
using Domain.Courses;
using Domain.Offerings;
using Infrastructure.Activities;
using Infrastructure.Courses;
using NUnit.Framework;

namespace Tests.Infrastructure;

[TestFixture]
public sealed class OfferingActivityContextReaderTests
{
    private static readonly Guid ErsId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OibId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ErsOffering = Guid.Parse("33333333-0000-0000-0000-000000000001");
    private static readonly Guid OibOffering = Guid.Parse("33333333-0000-0000-0000-000000000002");
    private static readonly Guid ErsPreviousYear = Guid.Parse("33333333-0000-0000-0000-000000000003");

    private static readonly ICourseReader Courses = new InMemoryCourseReader(new[]
    {
        new Course(ErsId, "ERS", "Elementi razvoja softvera"),
        new Course(OibId, "OIB", "Osnove informacione bezbednosti")
    });

    private static readonly Offering[] Offerings =
    {
        new(ErsOffering, ErsId, "2026/27"),
        new(OibOffering, OibId, "2026/27"),
        new(ErsPreviousYear, ErsId, "2025/26")
    };

    [Test]
    public async Task ResolveAsync_ResolvesOfferingByCourseCodeAndIdSuffix()
    {
        var reader = CreateReader(Offerings);

        var lookup = await reader.ResolveAsync("ERS-2026-27-00001");

        Assert.Multiple(() =>
        {
            Assert.That(lookup.Status, Is.EqualTo(ActivityContextStatus.Found));
            Assert.That(lookup.Context!.Id, Is.EqualTo(ErsOffering));
        });
    }

    [Test]
    public async Task ResolveAsync_DistinguishesOfferingsOfTheSameCourseByYear()
    {
        var reader = CreateReader(Offerings);

        var current = await reader.ResolveAsync("ers-2026-27-00001");
        var previous = await reader.ResolveAsync("ERS-2025-26-00003");

        Assert.Multiple(() =>
        {
            Assert.That(current.Context!.Id, Is.EqualTo(ErsOffering));
            Assert.That(previous.Context!.Id, Is.EqualTo(ErsPreviousYear));
        });
    }

    [Test]
    public async Task ResolveAsync_ResolvesOfferingOfAnotherCourse()
    {
        var reader = CreateReader(Offerings);

        var lookup = await reader.ResolveAsync("OIB-2026-27-00002");

        Assert.That(lookup.Context!.Id, Is.EqualTo(OibOffering));
    }

    [Test]
    public async Task ResolveAsync_DistinguishesSameIdSuffixInDifferentYears()
    {
        // Pet znakova identifikatora je isto (00001) — bez godine ove dve oznake bi se sudarile.
        var currentYear = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaa00001");
        var nextYear = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbb00001");
        var reader = CreateReader(new[]
        {
            new Offering(currentYear, ErsId, "2026/27"),
            new Offering(nextYear, ErsId, "2027/28")
        });

        var first = await reader.ResolveAsync("ERS-2026-27-00001");
        var second = await reader.ResolveAsync("ERS-2027-28-00001");
        var withoutYear = await reader.ResolveAsync("ERS-00001");

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(ActivityContextStatus.Found));
            Assert.That(first.Context!.Id, Is.EqualTo(currentYear));
            Assert.That(second.Status, Is.EqualTo(ActivityContextStatus.Found));
            Assert.That(second.Context!.Id, Is.EqualTo(nextYear));
            Assert.That(withoutYear.Status, Is.EqualTo(ActivityContextStatus.NotFound));
        });
    }

    [Test]
    public async Task ResolveAsync_DoesNotResolveCatalogCourseSlug()
    {
        var reader = CreateReader(Offerings);

        var lookup = await reader.ResolveAsync("ERS-11111");

        Assert.That(lookup.Status, Is.EqualTo(ActivityContextStatus.NotFound));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("ERS")]
    public async Task ResolveAsync_ReturnsNotFoundForMissingOrIncompleteSlug(string? slug)
    {
        var reader = CreateReader(Offerings);

        var lookup = await reader.ResolveAsync(slug);

        Assert.That(lookup.Status, Is.EqualTo(ActivityContextStatus.NotFound));
    }

    [Test]
    public async Task ResolveAsync_ReturnsCanceledForCanceledRequest()
    {
        var reader = CreateReader(Offerings);
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        var lookup = await reader.ResolveAsync("ERS-2026-27-00001", source.Token);

        Assert.That(lookup.Status, Is.EqualTo(ActivityContextStatus.Canceled));
    }

    [Test]
    public async Task ResolveAsync_ReturnsAmbiguousWhenTwoOfferingsShareTheSlug()
    {
        // Dva izvođenja istog predmeta u istoj godini ne smeju da postoje (ST-004), ali
        // ako se ipak pojave, oznaka ostaje dvosmislena umesto da tiho izabere jedno.
        var first = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaa00001");
        var second = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbb00001");
        var reader = CreateReader(new[]
        {
            new Offering(first, ErsId, "2026/27"),
            new Offering(second, ErsId, "2026/27")
        });

        var lookup = await reader.ResolveAsync("ERS-2026-27-00001");

        Assert.Multiple(() =>
        {
            Assert.That(lookup.Status, Is.EqualTo(ActivityContextStatus.Ambiguous));
            Assert.That(lookup.Context, Is.Null);
        });
    }

    [Test]
    public async Task ResolveAsync_SkipsOfferingsWithoutCourse()
    {
        var orphan = Guid.Parse("44444444-0000-0000-0000-000000000001");
        var reader = CreateReader(new[] { new Offering(orphan, Guid.NewGuid(), "2026/27") });

        var lookup = await reader.ResolveAsync("ERS-2026-27-00001");

        Assert.That(lookup.Status, Is.EqualTo(ActivityContextStatus.NotFound));
    }

    private static OfferingActivityContextReader CreateReader(IEnumerable<Offering> offerings) =>
        new(Courses, offerings);
}
