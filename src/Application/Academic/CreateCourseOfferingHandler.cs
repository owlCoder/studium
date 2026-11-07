using Application.Courses;
using Domain.Academic;

namespace Application.Academic;

public sealed class CreateCourseOfferingHandler
{
    private readonly ICourseOfferingRepository _offerings;
    private readonly IAcademicYearRepository _years;
    private readonly ICourseReader _courses;

    public CreateCourseOfferingHandler(
        ICourseOfferingRepository offerings,
        IAcademicYearRepository years,
        ICourseReader courses)
    {
        _offerings = offerings;
        _years = years;
        _courses = courses;
    }

    public async Task<Guid> HandleAsync(
        CreateCourseOfferingCommand command,
        CancellationToken cancellationToken = default)
    {
        var allCourses = await _courses.ListAsync(cancellationToken);
        var course = allCourses.FirstOrDefault(c => c.Id == command.CourseId)
            ?? throw new InvalidOperationException("Predmet ne postoji.");

        if (!course.IsActive)
        {
            throw new InvalidOperationException(
                "Deaktiviran predmet ne može imati novo izvođenje.");
        }

        var year = await _years.GetByIdAsync(command.AcademicYearId, cancellationToken)
            ?? throw new InvalidOperationException("Akademska godina ne postoji.");

        if (await _offerings.ExistsAsync(course.Id, year.Id, cancellationToken))
        {
            throw new InvalidOperationException(
                "Izvođenje tog predmeta u toj akademskoj godini već postoji.");
        }

        var offering = new CourseOffering(Guid.NewGuid(), course.Id, year.Id);
        await _offerings.AddAsync(offering, cancellationToken);
        return offering.Id;
    }
}
