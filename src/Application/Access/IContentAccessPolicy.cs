namespace Application.Access;

/// <summary>
/// Zajednička pravila pristupa sadržaju izvođenja za pretragu (ST-018)
/// i obaveštenja (ST-017). U KT1 postoji samo ugovor; pravila se
/// implementiraju u narednoj kontrolnoj tački.
/// </summary>
public interface IContentAccessPolicy
{
    /// <summary>
    /// Da li korisnik sme da vidi sadržaj datog izvođenja u datom stanju.
    /// </summary>
    Task<bool> CanViewAsync(
        UserContext user,
        Guid offeringId,
        ContentState state,
        CancellationToken cancellationToken = default);
}
