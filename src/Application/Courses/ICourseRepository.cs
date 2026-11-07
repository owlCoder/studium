using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
}
