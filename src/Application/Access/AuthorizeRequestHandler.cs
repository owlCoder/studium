using Domain.Users;

namespace Application.Access;

public sealed class AuthorizeRequestHandler
{
    private readonly IAccountReader _accounts;

    public AuthorizeRequestHandler(IAccountReader accounts)
    {
        _accounts = accounts;
    }

    public async Task<AccessDecision> HandleAsync(
        Guid? userId,
        IReadOnlyCollection<UserRole> allowedRoles,
        CancellationToken cancellationToken = default)
    {
        if (userId is null || userId == Guid.Empty)
            return AccessDecision.Unauthenticated;

        // :itanje iz baze podataka svaki put
        var account = await _accounts.FindByIdAsync(userId.Value, cancellationToken);

        if (account is null || !account.IsActive || !account.IsActivated)
            return AccessDecision.Unauthenticated;

        return allowedRoles.Contains(account.Role)
            ? AccessDecision.Allowed
            : AccessDecision.Forbidden;
    }
}
