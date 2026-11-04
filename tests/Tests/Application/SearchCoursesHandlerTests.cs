using Moq;
using NUnit.Framework;
using Application.Courses;
using Domain.Courses;

namespace Tests.Application;

[TestFixture]
public sealed class SearchCoursesHandlerTests
{
    private static Course CourseOf(string code, string name, string description) =>
        new(Guid.NewGuid(), code, name, description);

    private static (SearchCoursesHandler Handler, Mock<ICourseReader> Reader) Create(
        params Course[] courses)
    {
        IReadOnlyList<Course> list = courses;

        var reader = new Mock<ICourseReader>();
        reader.Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);

        return (new SearchCoursesHandler(reader.Object), reader);
    }

    [Test]
    public async Task HandleAsync_FindsCourseByCode_IgnoringCase()
    {
        var (handler, reader) = Create(
            CourseOf("ERS", "Razvoj softvera", "Opis predmeta"),
            CourseOf("OIB", "Bezbednost", "Opis predmeta"));

        var result = await handler.HandleAsync("ers");

        Assert.That(result.Select(x => x.Code), Is.EqualTo(new[] { "ERS" }));
        reader.Verify(x => x.ListAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_FindsCourseByPartOfName_IgnoringCase()
    {
        var (handler, _) = Create(
            CourseOf("ERS", "Elementi razvoja softvera", "Opis predmeta"),
            CourseOf("OIB", "Osnove informacione bezbednosti", "Opis predmeta"));

        var result = await handler.HandleAsync("BEZB");

        Assert.That(result.Select(x => x.Code), Is.EqualTo(new[] { "OIB" }));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public async Task HandleAsync_ReturnsAllCourses_WhenQueryIsBlank(string? query)
    {
        var (handler, _) = Create(
            CourseOf("OIB", "Bezbednost", "Opis predmeta"),
            CourseOf("ERS", "Razvoj softvera", "Opis predmeta"));

        var result = await handler.HandleAsync(query);

        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task HandleAsync_ReturnsEmptyCollection_WhenNothingMatches()
    {
        var (handler, _) = Create(CourseOf("ERS", "Razvoj softvera", "Opis predmeta"));

        var result = await handler.HandleAsync("nepostojeci");

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task HandleAsync_SortsResultsByCourseCode()
    {
        var (handler, _) = Create(
            CourseOf("OIB", "Bezbednost", "Opis predmeta"),
            CourseOf("ERS", "Razvoj softvera", "Opis predmeta"));

        var result = await handler.HandleAsync(null);

        Assert.That(result.Select(x => x.Code), Is.EqualTo(new[] { "ERS", "OIB" }));
    }

    [Test]
    public async Task HandleAsync_ReturnsIdCodeAndName()
    {
        var id = Guid.NewGuid();
        var (handler, _) = Create(new Course(id, "ERS", "Razvoj softvera", "Opis predmeta"));

        var result = await handler.HandleAsync("ers");

        Assert.Multiple(() =>
        {
            Assert.That(result[0].Id, Is.EqualTo(id));
            Assert.That(result[0].Code, Is.EqualTo("ERS"));
            Assert.That(result[0].Name, Is.EqualTo("Razvoj softvera"));
            Assert.That(result[0].Description, Is.EqualTo("Opis predmeta"));
        });
    }

    [Test]
    public void HandleAsync_DoesNotSwallowReaderFailure()
    {
        var reader = new Mock<ICourseReader>();
        reader.Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Izvor podataka nije dostupan."));

        var handler = new SearchCoursesHandler(reader.Object);

        Assert.That(
            async () => await handler.HandleAsync("ers"),
            Throws.TypeOf<InvalidOperationException>());
    }
}
