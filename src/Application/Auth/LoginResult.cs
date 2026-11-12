using Application.Sessions;

namespace Application.Auth;

/// <summary>
/// Razlozi odbijanja prijave.
/// InvalidInput = neispravan unos,
/// InvalidCredentials = neispunjen poslovni uslov.
/// </summary>
public enum LoginFailure
{
    /// <summary>Prazno korisničko ime ili lozinka.</summary>
    InvalidInput,

    /// <summary>
    /// Nepoznato ime, pogrešna lozinka,
    /// nalog bez završene aktivacije ILI deaktiviran nalog.
    /// </summary>
    InvalidCredentials
}

/// <summary>
/// Ishod prijave: uspeh sa identitetom i izdatom sesijom ILI razlog odbijanja.
/// Odbijena prijava nikada ne nosi sesiju.
/// </summary>
public sealed class LoginResult
{
    public bool Succeeded { get; }

    public AuthenticatedIdentity? Identity { get; }

    /// <summary>Token i rok isteka; postoji samo kad je prijava uspela.</summary>
    public IssuedSession? Session { get; }

    public LoginFailure? Failure { get; }

    private LoginResult(
        bool succeeded,
        AuthenticatedIdentity? identity,
        IssuedSession? session,
        LoginFailure? failure)
    {
        Succeeded = succeeded;
        Identity = identity;
        Session = session;
        Failure = failure;
    }

    public static LoginResult Success(AuthenticatedIdentity identity, IssuedSession session)
    {
        return new LoginResult(true, identity, session, null);
    }

    public static LoginResult Rejected(LoginFailure failure)
    {
        return new LoginResult(false, null, null, failure);
    }
}
