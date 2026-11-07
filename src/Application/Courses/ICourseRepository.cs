using System;
using System.Threading;
using System.Threading.Tasks;
using Domain.Courses;

namespace Application.Courses;

public interface ICourseRepository
{
    Task<bool> ExistsByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Course course,
        CancellationToken cancellationToken = default);

    Task<Course?> GetByIdAsync(
        Guid courseId,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Course course,
        CancellationToken cancellationToken = default);
}
