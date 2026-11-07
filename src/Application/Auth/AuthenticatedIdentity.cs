using Domain.Users;

namespace Application.Auth;

/// <summary>
/// Identitet uspešno prijavljenog korisnika. Namerno NE sadrži heš lozinke: ovaj tip napušta
/// Application sloj (ide ka API-ju i proveri ovlašćenja).
/// </summary>
public sealed record AuthenticatedIdentity(Guid UserId, string Username, UserRole Role);
