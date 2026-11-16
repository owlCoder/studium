using System.Security.Claims;
using Application.Access;
using Domain.Users;
using Microsoft.AspNetCore.Authorization;

namespace Api.Security;

public sealed class ActiveAdministratorRequirement : IAuthorizationRequirement
{
}

public sealed class ActiveAdministratorAuthorizationHandler
    : AuthorizationHandler<ActiveAdministratorRequirement>
{
    private readonly IAccountReader _accounts;

    public ActiveAdministratorAuthorizationHandler(IAccountReader accounts)
    {
        _accounts = accounts;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ActiveAdministratorRequirement requirement)
    {
        var userIdValue = context.User.FindFirst(
            ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return;
        }

        var account = await _accounts.FindByIdAsync(userId);

        if (account is not null
            && account.IsActive
            && account.IsActivated
            && account.Role == UserRole.Administrator)
        {
            context.Succeed(requirement);
        }
    }
}
