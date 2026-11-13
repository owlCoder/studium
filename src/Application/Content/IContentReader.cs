using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Content;

public interface IContentReader
{
    Task<IReadOnlyList<CourseContent>> GetByCourseAsync(Guid courseId, CancellationToken cancellationToken = default);
}
