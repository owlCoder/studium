namespace Infrastructure;

using Domain;
using Application;
using Application.Submissions;

public class MockSubmissionOverviewService : ISubmissionOverviewService
{
    private readonly List<SubmissionContract> _submissions = new();
    private readonly List<MemberContract> _members = new();
    private readonly List<TaskContract> _tasks = new();
    private readonly List<Guid> _authorizedMemberIds = new();

    public Guid DefaultMemberId { get; }
    public Guid DefaultTaskId { get; }

    public MockSubmissionOverviewService()
    {
        DefaultMemberId = Guid.NewGuid();
        DefaultTaskId = Guid.NewGuid();

        _authorizedMemberIds.Add(DefaultMemberId);

        var reviewedStudentId = Guid.NewGuid();
        var studentWithoutSubmissionId = Guid.NewGuid();

        // Mock pretpostavka: svi članovi u _members su studenti upisani na izvođenje zadatka.
        _members.Add(new MemberContract(DefaultMemberId, "Marko Marković", "Developer"));
        _members.Add(new MemberContract(reviewedStudentId, "Jovana Jovanović", "Student"));
        _members.Add(new MemberContract(studentWithoutSubmissionId, "Ana Anić", "Student"));

        _tasks.Add(new TaskContract(DefaultTaskId, "Implementacija Auth servisa", "Opis...", DateTime.UtcNow.AddDays(3)));

        _submissions.Add(new SubmissionContract(
            Guid.NewGuid(),
            DefaultTaskId,
            DefaultMemberId,
            "https://github.com/org/repo/pull/1",
            DateTime.UtcNow,
            SubmissionStatus.Pending
        ));
        _submissions.Add(new SubmissionContract(
            Guid.NewGuid(),
            DefaultTaskId,
            reviewedStudentId,
            "https://github.com/org/repo/pull/2",
            DateTime.UtcNow.AddHours(-2),
            SubmissionStatus.Accepted
        ));
        // Ana Anić namerno nema predaju -> broji se u "Bez predaje".
    }

    public Task<SubmissionOverviewDto?> GetSubmissionOverviewAsync(Guid submissionId, Guid requestingMemberId)
    {
        EnsureAuthorized(requestingMemberId);

        var submission = _submissions.FirstOrDefault(s => s.SubmissionId == submissionId);
        if (submission == null) return Task.FromResult<SubmissionOverviewDto?>(null);

        return Task.FromResult<SubmissionOverviewDto?>(ToOverview(submission));
    }

    public Task<IReadOnlyList<SubmissionOverviewDto>> GetSubmissionsForTaskAsync(Guid taskId, Guid requestingMemberId)
    {
        EnsureAuthorized(requestingMemberId);

        // Ista lista iz koje se računaju brojači: važeće predaje studenata članova.
        var lists = ClassifyTask(taskId);
        var result = lists.Unreviewed
            .Concat(lists.Reviewed)
            .Select(e => ToOverview(e.ValidSubmission!))
            .ToList();

        return Task.FromResult<IReadOnlyList<SubmissionOverviewDto>>(result);
    }

    public Task<SubmissionStatusCountsDto> GetStatusCountsForTaskAsync(Guid taskId, Guid requestingMemberId)
    {
        EnsureAuthorized(requestingMemberId);

        var lists = ClassifyTask(taskId);
        var counters = SubmissionReviewCounters.FromLists(lists);
        var reviewed = lists.Reviewed.Select(e => e.ValidSubmission!.Status).ToList();

        var counts = new SubmissionStatusCountsDto(
            WithoutSubmissionCount: counters.WithoutSubmission,
            UnreviewedCount: counters.Unreviewed,
            ReviewedCount: counters.Reviewed,
            TotalCount: counters.Unreviewed + counters.Reviewed,
            PendingCount: counters.Unreviewed,
            AcceptedCount: reviewed.Count(s => s == SubmissionStatus.Accepted),
            RejectedCount: reviewed.Count(s => s == SubmissionStatus.Rejected),
            NeedsRevisionCount: reviewed.Count(s => s == SubmissionStatus.NeedsRevision)
        );

        return Task.FromResult(counts);
    }

    private TaskSubmissionReviewLists ClassifyTask(Guid taskId) =>
        TaskSubmissionReviewClassifier.Classify(taskId, _members, _submissions);

    private void EnsureAuthorized(Guid requestingMemberId)
    {
        if (!_authorizedMemberIds.Contains(requestingMemberId))
            throw new UnauthorizedAccessException("Nemate pravo pristupa ovom zapisu.");
    }

    private SubmissionOverviewDto ToOverview(SubmissionContract submission)
    {
        var member = _members.FirstOrDefault(m => m.MemberId == submission.MemberId);
        var task = _tasks.FirstOrDefault(t => t.TaskId == submission.TaskId);

        return new SubmissionOverviewDto(
            submission.SubmissionId,
            task?.Title ?? "Nepoznat zadatak",
            member?.FullName ?? "Nepoznat član",
            submission.ContentUrl,
            submission.SubmittedAt,
            submission.Status
        );
    }
}
