using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Groups;
using Domain.Entities;

namespace Application.Content;

public record GetStudentContentQuery(Guid StudentId, Guid CourseId);

public class GetStudentContentHandler
{
    private readonly IContentReader _contentReader;
    private readonly IStudentGroupReader _studentGroupReader;

    public GetStudentContentHandler(
        IContentReader contentReader,
        IStudentGroupReader studentGroupReader)
    {
        _contentReader = contentReader;
        _studentGroupReader = studentGroupReader;
    }

    public async Task<IReadOnlyList<CourseContent>> Handle(GetStudentContentQuery request, CancellationToken cancellationToken)
    {
        var allCourseContent = await _contentReader.GetByCourseAsync(request.CourseId, cancellationToken);
        var studentActiveGroupIds = await _studentGroupReader.GetActiveGroupIdsForStudentAsync(request.StudentId, cancellationToken);

        
        var filteredContent = allCourseContent.Where(content =>
            content.TargetGroupId == null ||
            studentActiveGroupIds.Contains(content.TargetGroupId.Value)
        ).ToList();

        return filteredContent;
    }
}
