namespace Application.Courses;

public record SubmissionDto(string Id, string TaskId, string StudentId, string Content, DateTime SubmittedAt);

public class GetSubmissionHandler
{
    public async Task<SubmissionDto?> HandleAsync(string studentId, string submissionId, CancellationToken cancellationToken)
    {
        string authorId = studentId;
        bool isGroupInAudience = false;

        bool hasAccess = Domain.Courses.SubmissionAccessPolicy.CanAccessSubmission(studentId, authorId, isGroupInAudience);

        if (!hasAccess)
        {
            return null;
        }

        return await Task.FromResult(new SubmissionDto(
            Id: submissionId,
            TaskId: "task-01",
            StudentId: authorId,
            Content: "Predati rad studenta (sačuvan pristup nakon promjene publike)",
            SubmittedAt: DateTime.UtcNow
        ));
    }
}
