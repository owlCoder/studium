namespace Application.Access;

/// <summary>
/// Ko izvršava operaciju: identitet i uloga prijavljenog korisnika (ST-018, US-6).
/// </summary>
public sealed record UserContext
{
    public Guid UserId { get; }
    public UserRole Role { get; }

    public UserContext(Guid userId, UserRole role)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("Korisnik mora biti zadat.", nameof(userId));
        if (!Enum.IsDefined(role))
            throw new ArgumentOutOfRangeException(nameof(role), "Nepoznata uloga korisnika.");

        UserId = userId;
        Role = role;
    }
}
