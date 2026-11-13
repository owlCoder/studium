using Domain.Staff;

namespace Application.Staff;

public interface ITeachingAssignmentRepository
{
    Task<TeachingAssignment?> GetActiveAsync(Guid offeringId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TeachingAssignment>> ListActiveByOfferingAsync(Guid offeringId, CancellationToken cancellationToken = default);
    Task AddAsync(TeachingAssignment assignment, CancellationToken cancellationToken = default);
    Task UpdateAsync(TeachingAssignment assignment, CancellationToken cancellationToken = default);
}
