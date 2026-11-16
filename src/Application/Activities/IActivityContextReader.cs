namespace Application.Activities;

/// <summary>
/// Port koji razrešava oznaku iz URL-a u <see cref="ActivityContext"/>. Danas
/// memorijski adapter razrešava oznaku predmeta oblika <c>OZNAKA-xxxxx</c> preko
/// <c>ICourseReader</c>; kada ST-004 uvede izvođenja predmeta, dovoljno je zameniti
/// implementaciju porta, bez izmene domena, repozitorijuma i slučajeva upotrebe.
/// Implementacije ne bacaju izuzetke.
/// </summary>
public interface IActivityContextReader
{
    /// <summary>
    /// Razrešava oznaku konteksta. Nepoznata oznaka daje
    /// <see cref="ActivityContextStatus.NotFound"/>, dvosmislena
    /// <see cref="ActivityContextStatus.Ambiguous"/>, a otkazan zahtev
    /// <see cref="ActivityContextStatus.Canceled"/>.
    /// </summary>
    Task<ActivityContextLookup> ResolveAsync(
        string? contextSlug,
        CancellationToken cancellationToken = default);
}
