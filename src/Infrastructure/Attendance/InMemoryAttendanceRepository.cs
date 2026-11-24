using System.Collections.Concurrent;
using Application.Attendance;
using Domain.Attendance;

namespace Infrastructure.Attendance;

/// <summary>
/// Memorijska zamena skladišta prisustva, samo za testove i demo.
/// Bezbedna za istovremene unose: za jedan termin postoji jedna evidencija.
/// </summary>
public sealed class InMemoryAttendanceRepository : IAttendanceRepository
{
    private readonly ConcurrentDictionary<Guid, SessionAttendance> _bySession = new();

    public Task<SessionAttendance?> GetAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(
            _bySession.TryGetValue(sessionId, out var attendance) ? attendance : null);
    }

    public Task<SessionAttendance> GetOrCreateAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(
            _bySession.GetOrAdd(sessionId, id => new SessionAttendance(id)));
    }

    public Task SaveAsync(
        SessionAttendance attendance,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _bySession[attendance.SessionId] = attendance;
        return Task.CompletedTask;
    }
}
