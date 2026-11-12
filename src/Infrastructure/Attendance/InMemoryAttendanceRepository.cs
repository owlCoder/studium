using Application.Attendance;
using Domain.Attendance;

namespace Infrastructure.Attendance;

/// <summary>
/// Memorijska zamena skladišta prisustva, samo za testove i demo.
/// </summary>
public sealed class InMemoryAttendanceRepository : IAttendanceRepository
{
    private readonly Dictionary<Guid, SessionAttendance> _bySession = new();

    public Task<SessionAttendance?> GetAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(
            _bySession.TryGetValue(sessionId, out var attendance) ? attendance : null);
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
