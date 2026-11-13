using Domain.Courses;

namespace Application.Courses;

/// <summary>
/// Port: učitavanje i čuvanje predmeta radi promene aktivnosti (aktivacija  i deaktivacija).
/// </summary>
public interface ICourseStatusRepository
{
    /// <returns>Predmet ili <c>null</c> ako ne postoji.</returns>
    Task<Course?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task SaveAsync(Course course, CancellationToken cancellationToken = default);
}
