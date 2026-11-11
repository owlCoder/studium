using Application.Time;

namespace Application.Sessions;

/// <summary>
/// Pronalazi korisnika na osnovu tokena sesije. 
/// </summary>
public sealed class ResolveSessionHandler
{
    private readonly ISessionStore _store;
    private readonly ISessionTokenProvider _tokens;
    private readonly IClock _clock;

    public ResolveSessionHandler(
        ISessionStore store,
        ISessionTokenProvider tokens,
        IClock clock)
    {
        _store = store;
        _tokens = tokens;
        _clock = clock;
    }

    /// <returns>Identifikator korisnika ili <c>null</c> ako sesija ne važi.</returns>
    public async Task<Guid?> HandleAsync(string? token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var session = await _store.FindByTokenHashAsync(_tokens.Hash(token), cancellationToken);

        if (session is null || session.IsExpiredAt(_clock.UtcNow))
        {
            return null;
        }

        return session.UserId;
    }
}
