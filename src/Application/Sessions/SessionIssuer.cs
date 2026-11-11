using Application.Time;
using Domain.Sessions;

namespace Application.Sessions;

/// <summary>
/// Izdaje sesiju posle uspešne prijave.
/// </summary>
public sealed class SessionIssuer : ISessionIssuer
{
    private readonly ISessionStore _store;
    private readonly ISessionTokenProvider _tokens;
    private readonly IClock _clock;
    private readonly SessionSettings _settings;

    public SessionIssuer(
        ISessionStore store,
        ISessionTokenProvider tokens,
        IClock clock,
        SessionSettings settings)
    {
        _store = store;
        _tokens = tokens;
        _clock = clock;
        _settings = settings;
    }

    public async Task<IssuedSession> IssueAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = _clock.UtcNow;
        string token = _tokens.Generate();

        // U skladište ide samo heš. Token se vraća pozivaocu i nigde se ne čuva.
        var session = new Session(
            _tokens.Hash(token), userId, now, now + _settings.Lifetime);

        await _store.AddAsync(session, cancellationToken);

        return new IssuedSession(token, session.ExpiresAt);
    }
}
