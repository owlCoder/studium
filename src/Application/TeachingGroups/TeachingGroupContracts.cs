using Domain.Entities;
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
        GroupType Type,
        int? Capacity = null
    );
}

namespace Application.Groups
{
    public interface IGroupMembershipRepository
    {
        Task AddAsync(GroupMembership membership, CancellationToken cancellationToken = default);
        Task<GroupMembership?> GetActiveAsync(Guid groupId, Guid studentId, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }

    public interface IGroupLookup
    {
        Task<bool> ExistsAsync(Guid groupId, CancellationToken cancellationToken = default);
    }

    public interface IStudentLookup
    {
        Task<bool> IsActiveAsync(Guid studentId, CancellationToken cancellationToken = default);
    }
}