using Application.AttendanceOverview;
using Domain.TeachingSessions;

namespace Infrastructure.AttendanceOverview;

/// <summary>
/// Memorijski adapter za čitanje termina (ST-034), samo za testove i demo.
/// Kada ST-031 obezbedi trajno skladište, ovaj adapter se zamenjuje stvarnim.
/// </summary>
public sealed class InMemoryTeachingSessionReader : ITeachingSessionReader
{
    private readonly List<TeachingSession> _sessions = new();

    /// <summary>Dodaje termin u memorijsko skladište; vraća isti objekat radi nizanja poziva.</summary>
    public InMemoryTeachingSessionReader Add(TeachingSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _sessions.Add(session);
        return this;
    }

    public Task<IReadOnlyList<TeachingSession>> ListByOfferingAsync(
        Guid offeringId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = _sessions
            .Where(s => s.OfferingId == offeringId)
            .ToArray();

        return Task.FromResult<IReadOnlyList<TeachingSession>>(result);
    }
}
