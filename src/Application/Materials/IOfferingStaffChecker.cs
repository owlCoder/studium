namespace Application.Materials;

/// <summary>
/// Port za proveru da li korisnik pripada nastavnom osoblju konkretnog
/// izvođenja predmeta (ST-005), iz ugla nastavnih materijala (ST-009).
/// Potpis namerno odgovara <c>ITeachingStaffQueries.IsStaffOnOfferingAsync</c>
/// tima ST-005, kako bi se po integraciji njihov servis povezao adapterom,
/// bez menjanja njihovog ugovora.
/// </summary>
public interface IOfferingStaffChecker
{
    /// <returns>
    /// <c>true</c> samo ako korisnik ima aktivno angažovanje na tom izvođenju;
    /// u svakom drugom slučaju (nepoznat korisnik, drugo izvođenje,
    /// uklonjeno angažovanje) <c>false</c>.
    /// </returns>
    Task<bool> IsStaffOnOfferingAsync(
        Guid userId,
        Guid offeringId,
        CancellationToken cancellationToken = default);
}
