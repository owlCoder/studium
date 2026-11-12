namespace Domain.Sessions;

/// <summary>
/// Sesija prijavljenog korisnika (ST-002).
/// Čuva se samo HEŠ tokena 
/// </summary>
public sealed class Session
{
    public string TokenHash { get; }

    public Guid UserId { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset ExpiresAt { get; }

    public Session(string tokenHash, Guid userId, DateTimeOffset createdAt, DateTimeOffset expiresAt)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new ArgumentException("Heš tokena je obavezan.", nameof(tokenHash));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("Korisnik mora biti zadat.", nameof(userId));
        }

        if (expiresAt <= createdAt)
        {
            throw new ArgumentException("Istek mora biti posle nastanka sesije.", nameof(expiresAt));
        }

        TokenHash = tokenHash;
        UserId = userId;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    /// <summary>Sesija važi do trenutka isteka; u samom trenutku isteka više ne važi.</summary>
    public bool IsExpiredAt(DateTimeOffset now)
    {
        return now >= ExpiresAt;
    }
}
