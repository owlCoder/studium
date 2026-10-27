using Application.Courses;
using Domain.Courses;

namespace Infrastructure.Courses;

/// <summary>
/// Memorijski adapter samo za početni primer; nema trajnog čuvanja podataka.
/// </summary>
public sealed class InMemoryCourseReader : ICourseReader
{
    private readonly IReadOnlyList<Course> _courses;

    public InMemoryCourseReader(IEnumerable<Course> courses)
    {
        _courses = courses.ToArray();
    }

    public Task<IReadOnlyList<Course>> ListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_courses);
    }
}
