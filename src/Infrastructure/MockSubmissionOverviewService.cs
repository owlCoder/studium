namespace Infrastructure;

using Domain;
using Application;

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

        _members.Add(new MemberContract(DefaultMemberId, "Marko Marković", "Developer"));
        _tasks.Add(new TaskContract(DefaultTaskId, "Implementacija Auth servisa", "Opis...", DateTime.UtcNow.AddDays(3)));

        _submissions.Add(new SubmissionContract(
            Guid.NewGuid(),
            DefaultTaskId,
            DefaultMemberId,
            "https://github.com/org/repo/pull/1",
            DateTime.UtcNow,
            SubmissionStatus.Pending
        ));
    }

    public Task<SubmissionOverviewDto?> GetSubmissionOverviewAsync(Guid submissionId, Guid requestingMemberId)
    {
        if (!_authorizedMemberIds.Contains(requestingMemberId))
            throw new UnauthorizedAccessException("Nemate pravo pristupa ovom zapisu.");

        var submission = _submissions.FirstOrDefault(s => s.SubmissionId == submissionId);
        if (submission == null) return Task.FromResult<SubmissionOverviewDto?>(null);

        var member = _members.FirstOrDefault(m => m.MemberId == submission.MemberId);
        var task = _tasks.FirstOrDefault(t => t.TaskId == submission.TaskId);

        var overview = new SubmissionOverviewDto(
            submission.SubmissionId,
            task?.Title ?? "Nepoznat zadatak",
            member?.FullName ?? "Nepoznat član",
            submission.ContentUrl,
            submission.SubmittedAt,
            submission.Status
        );

        return Task.FromResult<SubmissionOverviewDto?>(overview);
    }

    public Task<IReadOnlyList<SubmissionOverviewDto>> GetSubmissionsForTaskAsync(Guid taskId, Guid requestingMemberId)
    {
        if (!_authorizedMemberIds.Contains(requestingMemberId))
            throw new UnauthorizedAccessException("Nemate pravo pristupa ovom zapisu.");

        var result = _submissions
            .Where(s => s.TaskId == taskId)
            .Select(s =>
            {
                var member = _members.FirstOrDefault(m => m.MemberId == s.MemberId);
                var task = _tasks.FirstOrDefault(t => t.TaskId == s.TaskId);

                return new SubmissionOverviewDto(
                    s.SubmissionId,
                    task?.Title ?? "Nepoznat zadatak",
                    member?.FullName ?? "Nepoznat član",
                    s.ContentUrl,
                    s.SubmittedAt,
                    s.Status
                );
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<SubmissionOverviewDto>>(result);
    }

    public Task<SubmissionStatusCountsDto> GetStatusCountsForTaskAsync(Guid taskId, Guid requestingMemberId)
    {
        if (!_authorizedMemberIds.Contains(requestingMemberId))
            throw new UnauthorizedAccessException("Nemate pravo pristupa ovom zapisu.");

        var taskSubmissions = _submissions.Where(s => s.TaskId == taskId).ToList();

        var counts = new SubmissionStatusCountsDto(
            TotalCount: taskSubmissions.Count,
            PendingCount: taskSubmissions.Count(s => s.Status == SubmissionStatus.Pending),
            AcceptedCount: taskSubmissions.Count(s => s.Status == SubmissionStatus.Accepted),
            RejectedCount: taskSubmissions.Count(s => s.Status == SubmissionStatus.Rejected),
            NeedsRevisionCount: taskSubmissions.Count(s => s.Status == SubmissionStatus.NeedsRevision)
        );

        return Task.FromResult(counts);
    }
}