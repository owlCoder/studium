using Domain.TeachingSessions;
namespace Application.TeachingSessions;

public interface ISessionRepository
{
    Task AddAsync(
        TeachingSession session,
        CancellationToken cancellationToken = default);

    Task<TeachingSession?> GetByIdAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        TeachingSession session,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TeachingSession>> ListByOfferingAndDateAsync(
        Guid offeringId,
        DateOnly date,
        CancellationToken cancellationToken = default);
}
