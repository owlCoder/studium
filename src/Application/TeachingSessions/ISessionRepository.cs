using Domain.TeachingSessions;
namespace Application.TeachingSessions;

public interface ISessionRepository
{
    Task AddAsync(
        TeachingSession session,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TeachingSession>> ListByOfferingAndDateAsync(
        Guid offeringId,
        DateOnly date,
        CancellationToken cancellationToken = default);
}
