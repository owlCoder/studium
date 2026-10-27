using NUnit.Framework;
using Domain.Courses;
using Infrastructure.Courses;

namespace Tests.Infrastructure;

[TestFixture]
public sealed class InMemoryCourseReaderTests
{
    [Test]
    public async Task ListAsync_ReturnsSeededCourses()
    {
        var expected = new Course(Guid.NewGuid(), "ERS", "Elementi razvoja softvera");
        var reader = new InMemoryCourseReader(new[] { expected });

        var result = await reader.ListAsync();

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Id, Is.EqualTo(expected.Id));
    }

    [Test]
    public void ListAsync_RespectsCancellation()
    {
        var reader = new InMemoryCourseReader(Array.Empty<Course>());
        using var source = new CancellationTokenSource();
        source.Cancel();

        Assert.That(
            () => reader.ListAsync(source.Token),
            Throws.TypeOf<OperationCanceledException>());
    }
}
