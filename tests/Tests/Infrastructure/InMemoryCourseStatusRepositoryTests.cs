using NUnit.Framework;
using Domain.Courses;
using Infrastructure.Courses;

namespace Tests.Infrastructure;

[TestFixture]
public sealed class InMemoryCourseStatusRepositoryTests
{
    [Test]
    public async Task FindByIdAsync_ReturnsStoredCourse()
    {
        var course = new Course(Guid.NewGuid(), "ERS", "Elementi razvoja softvera");
        var repository = new InMemoryCourseStatusRepository(new[] { course });

        var result = await repository.FindByIdAsync(course.Id);

        Assert.That(result, Is.SameAs(course));
    }

    [Test]
    public async Task FindByIdAsync_ReturnsNull_ForUnknownId()
    {
        var repository = new InMemoryCourseStatusRepository(Array.Empty<Course>());

        var result = await repository.FindByIdAsync(Guid.NewGuid());

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task SaveAsync_MakesActivationVisibleThroughCourseReader()
    {
        var courses = new[]
        {
            new Course(Guid.NewGuid(), "ERS", "Elementi razvoja softvera", isActive: false)
        };
        var reader = new InMemoryCourseReader(courses);
        var repository = new InMemoryCourseStatusRepository(courses);

        var stored = await repository.FindByIdAsync(courses[0].Id);
        stored!.Activate();
        await repository.SaveAsync(stored);

        var listed = await reader.ListAsync();
        Assert.That(listed[0].IsActive, Is.True);
    }

    [Test]
    public void FindByIdAsync_RespectsCancellation()
    {
        var repository = new InMemoryCourseStatusRepository(Array.Empty<Course>());
        using var source = new CancellationTokenSource();
        source.Cancel();

        Assert.That(
            () => repository.FindByIdAsync(Guid.NewGuid(), source.Token),
            Throws.TypeOf<OperationCanceledException>());
    }
}
