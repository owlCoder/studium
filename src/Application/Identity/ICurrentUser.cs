namespace Application.Identity;

/// <summary>
/// Port prema ST-001/ST-002: ko je trenutno prijavljen korisnik.
/// Handleri iz ST-004 i ST-005 koriste ovaj interfejs za provjeru ovlašćenja.
/// </summary>
public interface ICurrentUser
{
    Guid Id { get; }
    string Username { get; }
    UserRole Role { get; }
}
