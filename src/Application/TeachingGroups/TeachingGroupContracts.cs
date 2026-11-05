using Domain.TeachingGroups;

namespace Application.TeachingGroups;

public record CreateTeachingGroupRequest(
    Guid CourseExecutionId,
    string Code,
    string Name,
    GroupType Type
);

public record TeachingGroupResponse(
    Guid Id,
    Guid CourseExecutionId,
    string Code,
    string Name,
    GroupType Type
);

public interface ITeachingGroupRepository
{
    Task<TeachingGroupResponse> CreateAsync(TeachingGroup group, CancellationToken cancellationToken = default);
    Task<IEnumerable<TeachingGroupResponse>> GetByCourseExecutionIdAsync(Guid courseExecutionId, CancellationToken cancellationToken = default);
}