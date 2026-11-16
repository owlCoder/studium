using Domain.TeachingGroups;

namespace Application.TeachingGroups
{
    public interface ITeachingGroupRepository
    {
        Task<TeachingGroupResponse> CreateAsync(TeachingGroup group, CancellationToken cancellationToken = default);
        Task<IEnumerable<TeachingGroupResponse>> GetByCourseExecutionIdAsync(Guid courseExecutionId, CancellationToken cancellationToken = default);
    }

    public record CreateTeachingGroupRequest(
        Guid CourseExecutionId,
        string Code,
        string Name,
        string Type,
        int? Capacity = null
    );

    public record TeachingGroupResponse(
        Guid Id,
        Guid CourseExecutionId,
        string Code,
        string Name,
        Domain.TeachingGroups.GroupType Type,
        int? Capacity = null
    );
}