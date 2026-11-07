using Domain.Users;

namespace Application.Auth;

/// <summary>
/// Port: pronalaženje korisnika po korisničkom imenu za potrebe prijave.
/// </summary>
public interface IUserLookup
{
    /// <param name="normalizedUsername">Ime već normalizovano preko <see cref="User.NormalizeUsername"/>.</param>
    /// <returns>Korisnik ili <c>null</c> ako ne postoji.</returns>
    Task<User?> FindByUsernameAsync(string normalizedUsername,
        CancellationToken cancellationToken = default);
}
