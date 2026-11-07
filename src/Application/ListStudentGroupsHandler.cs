using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application; //11

public class ListStudentGroupsHandler
{
    private readonly Application.Groups.IGroupMembershipRepository _repository;
    private readonly IGroupRepository _groupRepository;

    public ListStudentGroupsHandler(
        Application.Groups.IGroupMembershipRepository repository,
        IGroupRepository groupRepository)
    {
        _repository = repository;
        _groupRepository = groupRepository;
    }

    public async Task<IReadOnlyList<Domain.Entities.Group>> HandleAsync(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var memberships = await _repository.GetActiveByStudentIdAsync(
            studentId,
            cancellationToken);

        var groups = new List<Domain.Entities.Group>();

        foreach (var membership in memberships)
        {
            var group = await _groupRepository.GetByIdAsync(
                membership.GroupId,
                cancellationToken);

            if (group != null)
            {
                groups.Add(group);
            }
        }

        return groups;
    }
}
