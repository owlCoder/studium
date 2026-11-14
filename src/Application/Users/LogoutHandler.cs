using Application.Sessions;

namespace Application.Users;

public sealed class LogoutHandler
{
    private readonly ISessionRevoker _sessionRevoker;
    private readonly ISessionTokenProvider _tokens;

    public LogoutHandler(ISessionRevoker sessionRevoker, ISessionTokenProvider tokens)
    {
        _sessionRevoker = sessionRevoker;
        _tokens = tokens;
    }

    /// <summary>Odjava: poništava samo sesiju čiji je token zadat.</summary>
    public async Task HandleAsync(
        string? token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new UnauthorizedAccessException("User is not authenticated.");

        await _sessionRevoker.RevokeSessionAsync(
            _tokens.Hash(token),
            cancellationToken);
    }
}
