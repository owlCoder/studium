namespace Domain.Users;

public sealed class User
{
    public Guid Id { get; }
    public string Username { get; }
    public UserRole Role { get; }

    // Dozvoljavamo privatni set da bi domen metoda mogla da izmeni stanje pri aktivaciji
    public string? PasswordHash { get; private set; }
    public bool IsActive { get; private set; }

    // Nova polja za aktivacioni token (opcionalna u konstruktoru zbog kompatibilnosti)
    public string? ActivationToken { get; private set; }
    public DateTime? TokenExpiration { get; private set; }

    public bool IsActivated => PasswordHash is not null;

    // Konstruktor sa opcionalnim tokenima da se ne bi pokvario nijedan postojeći poziv
    public User(
        Guid id,
        string username,
        UserRole role,
        string? passwordHash,
        bool isActive,
        string? activationToken = null,
        DateTime? tokenExpiration = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Identifikator mora biti zadat.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Korisničko ime je obavezno.", nameof(username));
        }

        Id = id;
        Username = username.Trim();
        Role = role;
        PasswordHash = passwordHash;
        IsActive = isActive;
        ActivationToken = activationToken;
        TokenExpiration = tokenExpiration;
    }

    public static string NormalizeUsername(string username)
    {
        return username.Trim().ToLowerInvariant();
    }

    public bool CanUseProtectedFunctions()
    {
        return IsActivated && IsActive;
    }

    /// <summary>
    /// Domen metoda za aktivaciju naloga: postavlja lozinku, aktivira nalog i poništava token.
    /// </summary>
    public void Activate(string passwordHash, string token)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Heš lozinke je obavezan.", nameof(passwordHash));
        }

        PasswordHash = passwordHash;
        IsActive = true;
        ActivationToken = null;
        TokenExpiration = null;
    }
}
