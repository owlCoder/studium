namespace Studium.Application.Courses;

/// <summary>
/// Demonstracija slučaja upotrebe i zavisnosti od porta, bez poslovne logike za ST-003.
/// </summary>
public sealed class ListCoursesHandler
{
    private readonly ICourseReader _reader;

    public ListCoursesHandler(ICourseReader reader)
    {
        _reader = reader;
    }

    public async Task<IReadOnlyList<CourseListItem>> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var courses = await _reader.ListAsync(cancellationToken);
        return courses
            .OrderBy(course => course.Code, StringComparer.OrdinalIgnoreCase)
            .Select(course => new CourseListItem(course.Code, course.Name))
            .ToArray();
    }
}
