namespace Application.Courses;

public sealed class SearchCoursesHandler
{
    private readonly ICourseReader _reader;

    public SearchCoursesHandler(ICourseReader reader)
    {
        _reader = reader;
    }

    public async Task<IReadOnlyList<CourseSearchResult>> HandleAsync(string? query,
                                                                     CancellationToken cancellationToken = default)
    {
        var term = query?.Trim();
        var courses = await _reader.ListAsync(cancellationToken);

        return courses.Where(courses => string.IsNullOrEmpty(term)
                           || courses.Code.Contains(term, StringComparison.OrdinalIgnoreCase)
                           || courses.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                           .OrderBy(courses => courses.Code, StringComparer.OrdinalIgnoreCase)
                           .Select(courses => new CourseSearchResult(courses.Id, courses.Code, courses.Name))
                           .ToArray();
    }
}
