using Application.Sessions;
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
///  5. sve u redu                            -> Success + izdata sesija
///
/// Sesiju izdaje <see cref="ISessionIssuer"/>, tek posle uspešne provere. Odbijena prijava
/// ne pravi nikakav zapis o sesiji.
/// </summary>
public sealed class LoginHandler
{
    // Sintetička vrednost, ne koristi se ni za jedan stvaran nalog.
    private const string DummyPassword = "lazna-lozinka-za-izjednacavanje-vremena";

    private readonly IUserLookup _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISessionIssuer _sessions;

    // Važeći heš istim algoritmom kao pravi nalozi, pa Verify traje isto koliko i inače.
    private readonly string _dummyPasswordHash;

    public LoginHandler(
        IUserLookup users,
        IPasswordHasher passwordHasher,
        ISessionIssuer sessions)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _sessions = sessions;
        _dummyPasswordHash = passwordHasher.Hash(DummyPassword);
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

        // Provera lozinke se izvršava uvek. Za nepoznat nalog ili nalog bez lozinke koristi se
        // lažni heš, a rezultat takve provere se ignoriše (nalog se svejedno odbija).
        string? realHash = user?.PasswordHash;
        bool passwordMatches = _passwordHasher.Verify(
            command.Password, realHash ?? _dummyPasswordHash);

        if (user is null || realHash is null || !passwordMatches)
        {
            return LoginResult.Rejected(LoginFailure.InvalidCredentials);
        }

        // Deaktiviran nalog ne sme da koristi zaštićene funkcije, a odgovor ga ne odaje.
        if (!user.CanUseProtectedFunctions())
        {
            return LoginResult.Rejected(LoginFailure.InvalidCredentials);
        }

        var identity = new AuthenticatedIdentity(user.Id, user.Username, user.Role);
        IssuedSession session = await _sessions.IssueAsync(user.Id, cancellationToken);

        return LoginResult.Success(identity, session);
    }
}
