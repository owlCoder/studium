namespace Application.Identity;

public static class CurrentUserExtensions
{
    /// <summary>
    /// Provjerava ulogu u servisnom sloju, a ne samo u meniju (specifikacija 2.1).
    /// </summary>
    public static void RequireRole(this ICurrentUser user, params UserRole[] allowedRoles)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (!allowedRoles.Contains(user.Role))
        {
            throw new UnauthorizedAccessException(
                $"Korisnik {user.Username} nema ovlašćenje za ovu operaciju.");
        }
    }
}
