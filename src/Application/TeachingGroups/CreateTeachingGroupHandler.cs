using Domain.TeachingGroups;

namespace Application.TeachingGroups;

public class CreateTeachingGroupHandler
{
    private readonly ITeachingGroupRepository _repository;

    public CreateTeachingGroupHandler(ITeachingGroupRepository repository)
    {
        _repository = repository;
    }

    public async Task<TeachingGroupResponse> HandleAsync(
        CreateTeachingGroupRequest request, 
        CancellationToken cancellationToken = default)
    {
        var group = new TeachingGroup(
            Guid.NewGuid(),
            request.CourseExecutionId,
            request.Code,
            request.Name,
            request.Type
        );

        return await _repository.CreateAsync(group, cancellationToken);
    }
}