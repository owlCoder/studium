namespace Application.Users;

public interface ISessionRevoker
{
    /// <summary>Odjava: poništava samo sesiju čiji je heš tokena zadat.</summary>
    Task RevokeSessionAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Promena lozinke: poništava sve sesije korisnika osim one sa zadatim hešom tokena
    /// (sesija sa koje je lozinka promenjena ostaje).
    /// </summary>
    Task RevokeOtherSessionsAsync(
        Guid userId,
        string keepTokenHash,
        CancellationToken cancellationToken = default);
}
