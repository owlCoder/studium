using Domain.TeachingSessions;

namespace Application.TeachingSessions;


public sealed class MarkSessionHeldHandler
{
    private readonly ISessionRepository _sessions;

    public MarkSessionHeldHandler(ISessionRepository sessions)
    {
        _sessions = sessions;
    }

    public async Task<TeachingSession> HandleAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessions.GetByIdAsync(sessionId, cancellationToken)
            ?? throw new InvalidOperationException("Termin ne postoji.");

        // Domen odbija prelaz iz Otkazan; tada se ništa ne čuva.
        session.MarkHeld();

        await _sessions.UpdateAsync(session, cancellationToken);
        return session;
    }
}
