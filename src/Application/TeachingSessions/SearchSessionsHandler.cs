using Domain.TeachingSessions;
namespace Application.TeachingSessions;

public sealed class SearchSessionsHandler
{
    private readonly ISessionRepository _sessions;

    public SearchSessionsHandler(ISessionRepository sessions)
    {
        _sessions = sessions;
    }

    public async Task<SessionSearchResult> HandleAsync(
        Guid offeringId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var sessions = await _sessions.ListByOfferingAndDateAsync(
            offeringId, date, cancellationToken);

        var active = sessions
            .Where(s => s.State != SessionState.Otkazan)
            .ToArray();
        var history = sessions
            .Where(s => s.State == SessionState.Otkazan)
            .ToArray();

        return new SessionSearchResult(active, history);
    }
}
