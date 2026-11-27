using Domain.TeachingSessions;

namespace Application.TeachingSessions;

/// <summary>
/// Evidentira planirani termin nastave (ST-031). Cilj može biti celo izvođenje
/// ili grupa koja pripada tom izvođenju. Termin se čuva tek kada prođu sve provere.
/// </summary>
public sealed class CreateSessionHandler
{
    private readonly ICourseOfferingReader _offerings;
    private readonly ISessionRepository _sessions;

    public CreateSessionHandler(ICourseOfferingReader offerings, ISessionRepository sessions)
    {
        _offerings = offerings;
        _sessions = sessions;
    }

    /// <summary>
    /// Kreira i čuva termin. Vraćeni termin nosi identifikator (<see cref="TeachingSession.Id"/>)
    /// pod kojim se kasnije pronalazi.
    /// </summary>
    public async Task<TeachingSession> HandleAsync(
        CreateSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!await _offerings.OfferingExistsAsync(command.OfferingId, cancellationToken))
            throw new InvalidOperationException("Izvođenje ne postoji.");

        if (command.GroupId is Guid groupId &&
            !await _offerings.GroupBelongsToOfferingAsync(
                command.OfferingId, groupId, cancellationToken))
            throw new InvalidOperationException("Grupa ne pripada tom izvođenju.");

        // Konstruktor odbija završetak koji nije posle početka; tada se ništa ne čuva.
        var session = new TeachingSession(
            Guid.NewGuid(),
            command.Kind,
            command.OfferingId,
            command.GroupId,
            command.StartsAt,
            command.EndsAt,
            command.Location);

        await _sessions.AddAsync(session, cancellationToken);
        return session;
    }
}
