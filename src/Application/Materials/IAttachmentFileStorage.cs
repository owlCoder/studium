namespace Application.Materials;

/// <summary>
/// Port za fizičko skladištenje sadržaja priloga nastavnog materijala (ST-009, US-04).
/// Skladište ne zna za materijal, izvođenje ni prava pristupa; to proveravaju slučajevi upotrebe.
/// </summary>
public interface IAttachmentFileStorage
{
    /// <summary>
    /// Čuva ceo sadržaj toka i vraća ključ pod kojim je sadržaj sačuvan.
    /// Ključ generiše skladište; izvorni naziv datoteke se ne koristi kao putanja.
    /// Ako čuvanje ne uspe, u skladištu ne ostaje delimično zapisana datoteka.
    /// </summary>
    Task<string> SaveAsync(Stream content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Otvara sačuvani sadržaj za čitanje ili vraća <c>null</c> ako sadržaj ne postoji.
    /// </summary>
    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Briše sačuvani sadržaj (ST-009, US-06; koristiće ga i US-07 pri uklanjanju priloga).
    /// Vraća <c>true</c> ako je sadržaj obrisan, a <c>false</c> ako pod ključem nema sadržaja
    /// ili ključ nije u formatu koji skladište generiše. Ponovljeno brisanje nije greška.
    /// </summary>
    Task<bool> DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}
