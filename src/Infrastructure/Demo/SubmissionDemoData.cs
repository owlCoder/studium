using Domain.Submissions;

namespace Infrastructure.Demo;

public static class SubmissionDemoData
{
    public static readonly Guid AssignmentId = Guid.Parse("12121212-0000-0000-0000-000000000001");
    public static readonly Guid StudentId = Guid.Parse("12121212-0000-0000-0000-000000000002");

    public static Submission CreateSubmission()
    {
        return new Submission(Guid.NewGuid(), AssignmentId, StudentId, "Primer predaje individualnog rada.",
            new List<Attachment> { new Attachment("primer.pdf", "application/pdf", 2048) },
            new DateTimeOffset(2026, 11, 23, 12, 0, 0, TimeSpan.FromHours(1)));
    }
}
