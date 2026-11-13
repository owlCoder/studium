using Domain.Staff;

namespace Application.Staff;

public interface ITeachingStaffQueries
{
    Task<bool> IsStaffOnOfferingAsync(Guid userId, Guid offeringId, CancellationToken cancellationToken = default);
    Task<bool> IsResponsibleTeacherAsync(Guid userId, Guid offeringId, CancellationToken cancellationToken = default);
    Task<bool> HasResponsibleTeacherAsync(Guid offeringId, CancellationToken cancellationToken = default);
}

public sealed class TeachingStaffQueries : ITeachingStaffQueries
{
    private readonly ITeachingAssignmentRepository _assignments;
    private readonly IUserDirectory _users;

    public TeachingStaffQueries(ITeachingAssignmentRepository assignments, IUserDirectory users)
    {
        _assignments = assignments;
        _users = users;
    }

    public async Task<bool> IsStaffOnOfferingAsync(Guid userId, Guid offeringId, CancellationToken cancellationToken = default)
    {
        var assignment = await _assignments.GetActiveAsync(offeringId, userId, cancellationToken);
        if (assignment is null)
        {
            return false;
        }

        var user = await _users.GetByIdAsync(userId, cancellationToken);
        return user is { IsActive: true };
    }

    public async Task<bool> IsResponsibleTeacherAsync(Guid userId, Guid offeringId, CancellationToken cancellationToken = default)
    {
        var assignment = await _assignments.GetActiveAsync(offeringId, userId, cancellationToken);
        if (assignment is null || assignment.Responsibility != Responsibility.ResponsibleTeacher)
        {
            return false;
        }

        var user = await _users.GetByIdAsync(userId, cancellationToken);
        return user is { IsActive: true };
    }

    public async Task<bool> HasResponsibleTeacherAsync(Guid offeringId, CancellationToken cancellationToken = default)
    {
        var list = await _assignments.ListActiveByOfferingAsync(offeringId, cancellationToken);
        return list.Any(a => a.Responsibility == Responsibility.ResponsibleTeacher);
    }
}
