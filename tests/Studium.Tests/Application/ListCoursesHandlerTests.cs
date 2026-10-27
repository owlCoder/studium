using Moq;
using NUnit.Framework;
using Studium.Application.Courses;
using Studium.Domain.Courses;

namespace Studium.Tests.Application;

[TestFixture]
public sealed class ListCoursesHandlerTests
{
    [Test]
    public async Task HandleAsync_SortsByCourseCode()
    {
        IReadOnlyList<Course> courses = new[]
        {
            new Course(Guid.NewGuid(), "OIB", "Bezbednost"),
            new Course(Guid.NewGuid(), "ERS", "Razvoj softvera")
        };

        var reader = new Mock<ICourseReader>();
        reader.Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(courses);

        var handler = new ListCoursesHandler(reader.Object);
        var result = await handler.HandleAsync();

        Assert.That(result.Select(x => x.Code), Is.EqualTo(new[] { "ERS", "OIB" }));
        reader.Verify(x => x.ListAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_ReturnsEmptyCollectionWhenThereAreNoCourses()
    {
        var reader = new Mock<ICourseReader>();
        reader.Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Course>());

        var result = await new ListCoursesHandler(reader.Object).HandleAsync();

        Assert.That(result, Is.Empty);
    }
}
