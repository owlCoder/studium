namespace Application.Identity;


public sealed class ResolveSessionUserHandler
{
    private readonly ISessionReader _sessions;
    private readonly IAccountStatusReader _accounts;

    public ResolveSessionUserHandler(ISessionReader sessions, IAccountStatusReader accounts)
    {
        _sessions = sessions;
        _accounts = accounts;
    }

    public async Task<Guid?> HandleAsync(
        string? sessionToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return null;
        }

        var userId = await _sessions.FindUserIdAsync(sessionToken, cancellationToken);
        if (userId is null || userId.Value == Guid.Empty)
        {
            return null;
        }

        var isActive = await _accounts.IsActiveAsync(userId.Value, cancellationToken);
        return isActive ? userId : null;
    }
}
