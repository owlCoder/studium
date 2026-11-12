namespace Application;

using Domain;

using Application.Submissions;

public interface ISubmissionOverviewService
{
    Task<SubmissionOverviewDto?> GetSubmissionOverviewAsync(Guid submissionId, Guid requestingMemberId);
    Task<IReadOnlyList<SubmissionOverviewDto>> GetSubmissionsForTaskAsync(Guid taskId, Guid requestingMemberId);
    Task<SubmissionStatusCountsDto> GetStatusCountsForTaskAsync(Guid taskId, Guid requestingMemberId);

    Task<TaskSubmissionReviewLists> GetTaskSubmissionReviewAsync(
    Guid taskId,
    Guid requestingMemberId);
}