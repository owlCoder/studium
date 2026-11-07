using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Content;
using Domain.Entities;

namespace Infrastructure.Content;

public class DummyContentReader : IContentReader
{
    public Task<IReadOnlyList<CourseContent>> GetByCourseAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        
        return Task.FromResult<IReadOnlyList<CourseContent>>(new List<CourseContent>());
    }
}
