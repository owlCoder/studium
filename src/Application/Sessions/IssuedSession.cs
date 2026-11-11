namespace Application.Sessions;

/// <summary>
/// Ono što korisnik dobija posle prijave: token i trenutak isteka. Token se vidi samo ovde,
/// jednom; u skladištu ostaje samo njegov heš.
/// </summary>
public sealed record IssuedSession(string Token, DateTimeOffset ExpiresAt);
