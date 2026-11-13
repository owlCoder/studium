using Application.TeachingGroups;
using Domain.TeachingGroups;

namespace Infrastructure.Demo;

public class InMemoryTeachingGroupRepository : ITeachingGroupRepository
{
    private readonly List<TeachingGroupResponse> _groups = new();

    public Task<TeachingGroupResponse> CreateAsync(
        TeachingGroup group, 
        CancellationToken cancellationToken = default)
    {
        var response = new TeachingGroupResponse(
            group.Id,
            group.CourseExecutionId,
            group.Code,
            group.Name,
            group.Type,
            group.Capacity
        );

        _groups.Add(response);
        return Task.FromResult(response);
    }

    public Task<IEnumerable<TeachingGroupResponse>> GetByCourseExecutionIdAsync(
        Guid courseExecutionId, 
        CancellationToken cancellationToken = default)
    {
        var result = _groups.Where(g => g.CourseExecutionId == courseExecutionId);
        return Task.FromResult<IEnumerable<TeachingGroupResponse>>(result);
    }
}