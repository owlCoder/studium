using Domain.TeachingSessions;

namespace Application.TeachingSessions;

/// <summary>
/// Evidentira planirani termin nastave (ST-031). Cilj može biti celo izvođenje
/// ili grupa koja pripada tom izvođenju.
/// </summary>
public sealed class CreateSessionHandler
{
    private readonly ICourseOfferingReader _offerings;

    public CreateSessionHandler(ICourseOfferingReader offerings)
    {
        _offerings = offerings;
    }

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

        return new TeachingSession(
            Guid.NewGuid(),
            command.Kind,
            command.OfferingId,
            command.GroupId,
            command.StartsAt,
            command.EndsAt,
            command.Location);
    }
}
