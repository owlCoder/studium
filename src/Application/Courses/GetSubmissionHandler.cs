namespace Application.Courses;

public record SubmissionDto(string Id, string TaskId, string StudentId, string Content, DateTime SubmittedAt, bool IsGroupInAudience = true);

public class GetSubmissionResult
{
    public SubmissionDto? Submission { get; init; }
    public bool IsNotFound { get; init; }
    public bool IsForbidden { get; init; }

    public static GetSubmissionResult NotFound() => new() { IsNotFound = true };
    public static GetSubmissionResult Forbidden() => new() { IsForbidden = true };
    public static GetSubmissionResult Success(SubmissionDto submission) => new() { Submission = submission };

    public static implicit operator SubmissionDto?(GetSubmissionResult result) => result.Submission;
}

public class GetSubmissionHandler
{
    // In-memory repozitorijum sa poznatim sačuvanim predajama
    private static readonly List<SubmissionDto> DemoSubmissions = new()
    {
        new SubmissionDto("sub-01", "task-01", "student-autor-1", "Sadržaj rada 1", DateTime.UtcNow.AddDays(-2), true),
        new SubmissionDto("sub-02", "task-01", "student-autor-2", "Sadržaj rada 2", DateTime.UtcNow.AddDays(-1), false)
    };

    public async Task<GetSubmissionResult> HandleAsync(string requestingStudentId, string submissionId, CancellationToken cancellationToken = default)
    {
        // 1. Potraži rad po ID-ju u repozitorijumu
        var submission = DemoSubmissions.FirstOrDefault(s => s.Id == submissionId);

        // Za SVAKI nepostojeći ID vraćamo NotFound (404)
        if (submission is null)
        {
            return GetSubmissionResult.NotFound();
        }

        // 2. Provera prava pristupa po Pravilniku
        bool hasAccess = Domain.Courses.SubmissionAccessPolicy.CanAccessSubmission(
            requestingStudentId,
            submission.StudentId,
            submission.IsGroupInAudience
        );

        if (!hasAccess)
        {
            return GetSubmissionResult.Forbidden();
        }

        return await Task.FromResult(GetSubmissionResult.Success(submission));
    }
}
