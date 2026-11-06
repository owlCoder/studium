namespace Application;

using Domain;

public interface ISubmissionOverviewService
{
    Task<SubmissionOverviewDto?> GetSubmissionOverviewAsync(Guid submissionId, Guid requestingMemberId);
    Task<IReadOnlyList<SubmissionOverviewDto>> GetSubmissionsForTaskAsync(Guid taskId, Guid requestingMemberId);
    Task<SubmissionStatusCountsDto> GetStatusCountsForTaskAsync(Guid taskId, Guid requestingMemberId);
}