using Application.Users;
using Domain.Users;

namespace Application.Auth;

/// <summary>
/// Slučaj upotrebe "Prijava" (ST-002).
/// Odgovor ne otkriva stanje naloga: svako odbijanje zbog identiteta ili stanja naloga
/// daje isti razlog (InvalidCredentials).
///  1. prazan unos                           -> InvalidInput
///  2. nepoznato ime ILI nalog bez lozinke   -> InvalidCredentials
///     (nalog kome aktivacija nije završena nema lozinku, pa se ponaša kao nepoznat)
///  3. pogrešna lozinka                      -> InvalidCredentials
///  4. tačna lozinka, a nalog deaktiviran    -> InvalidCredentials
///  5. sve u redu                            -> Success
/// </summary>
public sealed class LoginHandler
{
    private readonly IUserLookup _users;
    private readonly IPasswordHasher _passwordHasher;

    public LoginHandler(IUserLookup users, IPasswordHasher passwordHasher)
    {
        _users = users;
        _passwordHasher = passwordHasher;
    }

    public async Task<LoginResult> HandleAsync(LoginCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Username) || string.IsNullOrEmpty(command.Password))
        {
            return LoginResult.Rejected(LoginFailure.InvalidInput);
        }

        // Poređenje imena bez razlike u veličini slova definiše domen, ne handler.
        string normalized = User.NormalizeUsername(command.Username);
        User? user = await _users.FindByUsernameAsync(normalized, cancellationToken);

        // Nepoznat korisnik ili nalog kome aktivacija nije završena (nema lozinku).
        if (user?.PasswordHash is not { } passwordHash)
        {
            return LoginResult.Rejected(LoginFailure.InvalidCredentials);
        }

        if (!_passwordHasher.Verify(command.Password, passwordHash))
        {
            return LoginResult.Rejected(LoginFailure.InvalidCredentials);
        }

        // Deaktiviran nalog ne sme da koristi zaštićene funkcije, a odgovor ga ne odaje.
        if (!user.CanUseProtectedFunctions())
        {
            return LoginResult.Rejected(LoginFailure.InvalidCredentials);
        }

        return LoginResult.Success(new AuthenticatedIdentity(user.Id, user.Username, user.Role));
    }
}
