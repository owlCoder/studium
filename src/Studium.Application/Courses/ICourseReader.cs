using Studium.Domain.Courses;

namespace Studium.Application.Courses;

/// <summary>
/// Čita samo demonstracione podatke. Kasnije ugovore oblikovati prema zahtevima.
/// </summary>
public interface ICourseReader
{
    Task<IReadOnlyList<Course>> ListAsync(CancellationToken cancellationToken = default);
}
