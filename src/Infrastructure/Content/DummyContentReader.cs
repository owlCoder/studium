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
        // Vraćamo dva izmišljena materijala 
        var mockSadrzaj = new List<CourseContent>
        {
            new CourseContent
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                Title = "Opšti materijal predmeta",
                Description = "Ovo vide svi studenti",
                TargetGroupId = null 
            },
            new CourseContent
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                Title = "Materijal za Grupu 1",
                Description = "Ovo vidi samo Grupa 1",
                TargetGroupId = Guid.Parse("11111111-1111-1111-1111-111111111111")
            }
        };

        return Task.FromResult<IReadOnlyList<CourseContent>>(mockSadrzaj);
    }
}
