using System.Security.Claims;

namespace Api.Identity;

/// <summary>
/// US-7.5: jedino mesto odakle endpointi uzimaju ID prijavljenog korisnika.
/// Claim postavlja isključivo SessionAuthenticationHandler, na osnovu sesije.
/// </summary>
public static class SessionUserExtensions
{
    public static bool TryGetSessionUserId(this ClaimsPrincipal user, out Guid userId)
    {
        userId = Guid.Empty;

        if (user.Identity?.IsAuthenticated != true
            || user.Identity.AuthenticationType != SessionAuthenticationHandler.SchemeName)
        {
            return false;
        }

        return Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out userId)
            && userId != Guid.Empty;
    }
}
