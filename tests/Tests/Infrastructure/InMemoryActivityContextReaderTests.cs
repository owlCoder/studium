using Application.Activities;
using Application.Courses;
using Domain.Courses;
using Infrastructure.Activities;
using Infrastructure.Courses;
using Moq;
using NUnit.Framework;

namespace Tests.Infrastructure;

[TestFixture]
public sealed class InMemoryActivityContextReaderTests
{
    private static readonly Guid SoftwareEngineeringId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid DuplicateId = Guid.Parse("22222222-2222-2222-2222-000000011111");

    private static Course SoftwareEngineering() =>
        new(SoftwareEngineeringId, "ERS", "Elementi razvoja softvera");

    private static InMemoryActivityContextReader CreateReader(params Course[] courses) =>
        new(new InMemoryCourseReader(courses));

    [Test]
    public async Task ResolveAsync_ReturnsContextForExistingSlug()
    {
        var reader = CreateReader(SoftwareEngineering());

        var lookup = await reader.ResolveAsync("ERS-11111");

        Assert.That(lookup.IsSuccess, Is.True);
        Assert.That(lookup.Context!.Id, Is.EqualTo(SoftwareEngineeringId));
    }

    [Test]
    public async Task ResolveAsync_IgnoresSlugCase()
    {
        var reader = CreateReader(SoftwareEngineering());

        var lookup = await reader.ResolveAsync("ers-11111");

        Assert.That(lookup.Status, Is.EqualTo(ActivityContextStatus.Found));
        Assert.That(lookup.Context!.Id, Is.EqualTo(SoftwareEngineeringId));
    }

    [TestCase("XXX-33333")]
    [TestCase("ERS")]
    [TestCase("")]
    [TestCase(null)]
    public async Task ResolveAsync_ReturnsNotFoundForUnknownSlug(string? slug)
    {
        var reader = CreateReader(SoftwareEngineering());

        var lookup = await reader.ResolveAsync(slug);

        Assert.Multiple(() =>
        {
            Assert.That(lookup.Status, Is.EqualTo(ActivityContextStatus.NotFound));
            Assert.That(lookup.Context, Is.Null);
            Assert.That(lookup.Error, Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public async Task ResolveAsync_ReturnsAmbiguousWhenSlugMatchesSeveralCourses()
    {
        var reader = CreateReader(
            SoftwareEngineering(),
            new Course(DuplicateId, "ERS", "Drugi predmet iste oznake"));

        var lookup = await reader.ResolveAsync("ERS-11111");

        Assert.Multiple(() =>
        {
            Assert.That(lookup.Status, Is.EqualTo(ActivityContextStatus.Ambiguous));
            Assert.That(lookup.Context, Is.Null);
        });
    }

    [Test]
    public async Task ResolveAsync_ReturnsCanceledWhenReaderThrows()
    {
        var courseReader = new Mock<ICourseReader>();
        courseReader.Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());
        var reader = new InMemoryActivityContextReader(courseReader.Object);

        var lookup = await reader.ResolveAsync("ERS-11111");

        Assert.Multiple(() =>
        {
            Assert.That(lookup.Status, Is.EqualTo(ActivityContextStatus.Canceled));
            Assert.That(lookup.Context, Is.Null);
            Assert.That(lookup.Error, Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public async Task ResolveAsync_ReturnsCanceledWithoutThrowing()
    {
        var reader = CreateReader(SoftwareEngineering());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var lookup = await reader.ResolveAsync("ERS-11111", cancellation.Token);

        Assert.Multiple(() =>
        {
            Assert.That(lookup.Status, Is.EqualTo(ActivityContextStatus.Canceled));
            Assert.That(lookup.Context, Is.Null);
        });
    }
}
