
namespace Application.Users;

public sealed class LogoutHandler
{
    private readonly ISessionRevoker _sessionRevoker;

    public LogoutHandler(ISessionRevoker sessionRevoker)
    {
        _sessionRevoker = sessionRevoker;
    }

    public async Task HandleAsync(
        Guid authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        if (authenticatedUserId == Guid.Empty)
            throw new UnauthorizedAccessException("User is not authenticated.");

        await _sessionRevoker.RevokeAsync(
            authenticatedUserId,
            cancellationToken);
    }
}
