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
/// Ishod prijave: uspeh sa identitetom ILI razlog odbijanja.
/// </summary>
public sealed class LoginResult
{
    public bool Succeeded { get; }

    public AuthenticatedIdentity? Identity { get; }

    public LoginFailure? Failure { get; }

    private LoginResult(bool succeeded, AuthenticatedIdentity? identity, LoginFailure? failure)
    {
        Succeeded = succeeded;
        Identity = identity;
        Failure = failure;
    }

    public static LoginResult Success(AuthenticatedIdentity identity)
    {
        return new LoginResult(true, identity, null);
    }

    public static LoginResult Rejected(LoginFailure failure)
    {
        return new LoginResult(false, null, failure);
    }
}
