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
    Inactive,
    Suspended
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

    public User(Guid id, string firstName, string lastName, string username, UserRole role)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("ID korisnika ne smije biti prazan.", nameof(id));
        }
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException("Ime je obavezno.", nameof(firstName));
        }
        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException("Prezime je obavezno.", nameof(lastName));
        }
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Korisničko ime je obavezno.", nameof(username));
        }
        Id = id;
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Username = username.Trim().ToLowerInvariant(); // Normalizacija
        Role = role;
    }

    public void Deactivate()
    {
        if (Status == UserStatus.Inactive)
        {
            throw new InvalidOperationException("Nalog je već deaktiviran.");
        }
        Status = UserStatus.Inactive;
    }
}
