using Domain.TeachingSessions;

namespace Application.TeachingSessions;

/// <summary>
/// Otkazuje sačuvan termin (ST-031). Otkazan termin ostaje u istoriji,
/// ne prikazuje se kao aktivna obaveza i ne prima nov unos prisustva.
/// Ranije uneti zapisi prisustva se ne brišu.
/// </summary>
public sealed class CancelSessionHandler
{
    private readonly ISessionRepository _sessions;

    public CancelSessionHandler(ISessionRepository sessions)
    {
        _sessions = sessions;
    }

    public async Task<TeachingSession> HandleAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessions.GetByIdAsync(sessionId, cancellationToken)
            ?? throw new InvalidOperationException("Termin ne postoji.");

        // Domen trenutno odbija otkazivanje održanog termina (otvoreno pitanje 2 za asistenta).
        session.Cancel();

        await _sessions.UpdateAsync(session, cancellationToken);
        return session;
    }
}
