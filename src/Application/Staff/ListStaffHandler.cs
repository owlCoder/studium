using Application.Academic;

namespace Application.Staff;

/// <summary>ST-005 (saradnici): pregled saradnika na izvođenju.</summary>
public sealed class ListStaffHandler
{
    private readonly ICourseOfferingRepository _offerings;
    private readonly ITeachingAssignmentRepository _assignments;
    private readonly IUserDirectory _users;
    private readonly ITeachingStaffQueries _staff;

    public ListStaffHandler(
        ICourseOfferingRepository offerings,
        ITeachingAssignmentRepository assignments,
        IUserDirectory users,
        ITeachingStaffQueries staff)
    {
        _offerings = offerings;
        _assignments = assignments;
        _users = users;
        _staff = staff;
    }

    public async Task<IReadOnlyList<StaffMemberDto>> HandleAsync(
        ListStaffQuery query,
        CancellationToken cancellationToken = default)
    {
        _ = await _offerings.GetByIdAsync(query.OfferingId, cancellationToken)
            ?? throw new NotFoundException("Izvođenje ne postoji.");

        if (!await _staff.IsStaffOnOfferingAsync(query.ActorId, query.OfferingId, cancellationToken))
        {
            throw new AccessDeniedException("Nemate pravo uvida u saradnike ovog izvođenja.");
        }

        var result = new List<StaffMemberDto>();
        foreach (var assignment in await _assignments.ListActiveByOfferingAsync(query.OfferingId, cancellationToken))
        {
            var user = await _users.GetByIdAsync(assignment.UserId, cancellationToken);
            if (user is null)
            {
                continue;
            }

            result.Add(new StaffMemberDto(
                user.Id, user.Username, user.FirstName, user.LastName, assignment.Responsibility));
        }

        return result
            .OrderBy(m => m.Responsibility)
            .ThenBy(m => m.LastName)
            .ThenBy(m => m.FirstName)
            .ToList();
    }
}
