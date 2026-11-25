using Application.TeachingSessions;
using Domain.TeachingSessions;
namespace Infrastructure.TeachingSessions;

public sealed class InMemorySessionRepository : ISessionRepository
{
    private readonly List<TeachingSession> _sessions = new();

    public Task AddAsync(
        TeachingSession session,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _sessions.Add(session);
        return Task.CompletedTask;
    }

    public Task<TeachingSession?> GetByIdAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_sessions.FirstOrDefault(s => s.Id == sessionId));
    }

    public Task UpdateAsync(
        TeachingSession session,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var index = _sessions.FindIndex(s => s.Id == session.Id);
        if (index < 0)
            throw new InvalidOperationException("Termin ne postoji.");
        _sessions[index] = session;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<TeachingSession>> ListByOfferingAndDateAsync(
        Guid offeringId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = _sessions
            .Where(s => s.OfferingId == offeringId &&
                        DateOnly.FromDateTime(s.StartsAt) == date)
            .ToArray();
        return Task.FromResult<IReadOnlyList<TeachingSession>>(result);
    }
}
