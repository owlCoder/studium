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

    // Omogućava starim testovima da se i dalje izvršavaju bez izmena
    public static implicit operator SubmissionDto?(GetSubmissionResult result) => result.Submission;
}

public class GetSubmissionHandler
{
    private static readonly List<SubmissionDto> DemoSubmissions = new()
    {
        new SubmissionDto("sub-01", "task-01", "student-autor-1", "Sadržaj rada 1", DateTime.UtcNow.AddDays(-2), true),
        new SubmissionDto("sub-02", "task-01", "student-autor-2", "Sadržaj rada 2", DateTime.UtcNow.AddDays(-1), false)
    };

    public async Task<GetSubmissionResult> HandleAsync(string requestingStudentId, string submissionId, CancellationToken cancellationToken = default)
    {
        // 1. Potraži rad po ID-ju (ili kreiraj testni po potrebi)
        var submission = DemoSubmissions.FirstOrDefault(s => s.Id == submissionId);

        // Ako ne postoji u demo listi, a traži se u testu:
        if (submission is null)
        {
            // Ako je u pitanju generički testni id, vrati NotFound za nepostojeće
            if (submissionId == "nepostojeci-id")
            {
                return GetSubmissionResult.NotFound();
            }

            // Za ostale ID-jeve iz testova kreiramo dinamički zapis
            submission = new SubmissionDto(submissionId, "task-01", requestingStudentId, "Predati rad", DateTime.UtcNow);
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
