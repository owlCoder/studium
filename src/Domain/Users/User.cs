namespace Domain.Users;

public enum UserRole
{
    Administrator,
    Nastavnik,
    Asistent,
    Student
}

public enum UserStatus
{
    Active,
    Deactivated
}

public sealed class User
{
    public Guid Id { get; }

    public string FirstName { get; }

    public string LastName { get; }

    public string Username { get; }

    public string? Email { get; private set; }

    public UserRole Role { get; private set; }

    public UserStatus Status { get; private set; } = UserStatus.Active;

    // Hash lozinke je null dok korisnik ne završi aktivaciju.
    // Čista lozinka se nikada ne čuva.
    public string? PasswordHash { get; private set; }

    // Pomoćna svojstva koja koriste ST-002 provera prijave i autorizacija.
    public bool IsActive => Status == UserStatus.Active;

    public bool IsActivated => PasswordHash is not null;

    public User(
        Guid id,
        string firstName,
        string lastName,
        string username,
        UserRole role,
        string? email = null,
        string? passwordHash = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "ID korisnika ne smije biti prazan.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException(
                "Ime je obavezno.", nameof(firstName));
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException(
                "Prezime je obavezno.", nameof(lastName));
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException(
                "Korisničko ime je obavezno.", nameof(username));
        }

        if (passwordHash is not null &&
            string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException(
                "Heš lozinke ne sme biti prazan.", nameof(passwordHash));
        }

        Id = id;
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Username = NormalizeUsername(username);
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        Role = role;
        PasswordHash = passwordHash;
    }

    // Jedno mesto za normalizaciju korisničkog imena.
    // "Stefan" i "stefan" predstavljaju isto korisničko ime.
    public static string NormalizeUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException(
                "Korisničko ime je obavezno.", nameof(username));
        }

        return username.Trim().ToLowerInvariant();
    }

    // Vraća nezavisnu kopiju korisnika.
    // Status i hash se prenose, ali se ne deli ista referenca objekta.
    public User Copy()
    {
        var copy = new User(
            Id,
            FirstName,
            LastName,
            Username,
            Role,
            Email,
            PasswordHash);

        if (Status == UserStatus.Deactivated)
        {
            copy.Deactivate();
        }

        return copy;
    }

    // Završetak aktivacije: aplikacioni sloj prethodno proverava
    // aktivacioni token i prosleđuje već izračunat hash lozinke.
    public void CompleteActivation(string passwordHash)
    {
        if (IsActivated)
        {
            throw new InvalidOperationException(
                "Nalog je već aktiviran.");
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException(
                "Heš lozinke ne sme biti prazan.", nameof(passwordHash));
        }

        PasswordHash = passwordHash;
    }

    // Nalog sme da koristi zaštićene funkcije samo ako je aktivan
    // i ako je završena njegova aktivacija.
    public bool CanUseProtectedFunctions()
    {
        return IsActive && IsActivated;
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            throw new InvalidOperationException(
                "Nalog je već deaktiviran.");
        }

        Status = UserStatus.Deactivated;
    }
}
