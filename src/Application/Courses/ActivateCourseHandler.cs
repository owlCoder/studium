namespace Application.Courses;

/// Slučaj upotrebe "Ponovna aktivacija predmeta" (ST-003).
/// Menja samo aktivnost kataloškog zapisa. Izvođenja i radovi se ne diraju.
/// Ko sme da pozove (Administrator) proverava ulazni sloj pre ovog handlera.
public sealed class ActivateCourseHandler
{
    private readonly ICourseStatusRepository _courses;

    public ActivateCourseHandler(ICourseStatusRepository courses)
    {
        _courses = courses;
    }

    public async Task<ActivateCourseResult> HandleAsync(Guid courseId,
        CancellationToken cancellationToken = default)
    {
        var course = await _courses.FindByIdAsync(courseId, cancellationToken);

        if (course is null)
        {
            return ActivateCourseResult.NotFound;
        }

        if (course.IsActive)
        {
            return ActivateCourseResult.AlreadyActive;
        }

        course.Activate();
        await _courses.SaveAsync(course, cancellationToken);

        return ActivateCourseResult.Activated;
    }
}
