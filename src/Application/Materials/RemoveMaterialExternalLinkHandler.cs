namespace Application.Materials;

/// <summary>
/// Zahtev za uklanjanje jedne spoljne veze iz materijala (ST-009, US-07).
/// </summary>
public sealed record RemoveMaterialExternalLinkCommand(
    Guid RequestingUserId,
    Guid MaterialId,
    Guid LinkId);

/// <summary>
/// Ishod uklanjanja spoljne veze (ST-009, US-07).
/// </summary>
public enum RemoveMaterialExternalLinkOutcome
{
    /// <summary>Veza je uklonjena iz materijala.</summary>
    Removed,

    /// <summary>Materijal ne postoji.</summary>
    MaterialNotFound,

    /// <summary>Veza ne postoji, pripada drugom materijalu ili je već uklonjena.</summary>
    LinkNotFound
}

/// <summary>
/// Slučaj upotrebe: nastavnik uklanja spoljnu vezu iz materijala (ST-009, US-07, tačka 1).
/// Redosled: materijal mora postojati → korisnik mora pripadati nastavnom osoblju
/// izvođenja materijala (ST-005) → veza mora postojati i pripadati tom materijalu →
/// veza se uklanja.
/// Veza se traži tek posle provere prava, pa korisnik bez prava ne može da sazna
/// koje veze postoje. Veza drugog materijala se ne može ukloniti preko ovog materijala.
/// Za razliku od priloga, veza nema sadržaj u skladištu, pa se ništa drugo ne briše.
/// </summary>
public sealed class RemoveMaterialExternalLinkHandler
{
    private readonly IMaterialRepository _materials;
    private readonly IMaterialExternalLinkRepository _links;
    private readonly IOfferingStaffChecker _staffChecker;

    public RemoveMaterialExternalLinkHandler(
        IMaterialRepository materials,
        IMaterialExternalLinkRepository links,
        IOfferingStaffChecker staffChecker)
    {
        ArgumentNullException.ThrowIfNull(materials);
        ArgumentNullException.ThrowIfNull(links);
        ArgumentNullException.ThrowIfNull(staffChecker);

        _materials = materials;
        _links = links;
        _staffChecker = staffChecker;
    }

    /// <exception cref="MaterialAccessDeniedException">
    /// Korisnik nije zadat ili ne pripada nastavnom osoblju izvođenja materijala.
    /// </exception>
    public async Task<RemoveMaterialExternalLinkOutcome> HandleAsync(
        RemoveMaterialExternalLinkCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var material = await _materials.GetByIdAsync(command.MaterialId, cancellationToken);
        if (material is null)
            return RemoveMaterialExternalLinkOutcome.MaterialNotFound;

        var isStaff = command.RequestingUserId != Guid.Empty
            && await _staffChecker.IsStaffOnOfferingAsync(
                command.RequestingUserId,
                material.CourseOfferingId,
                cancellationToken);

        if (!isStaff)
        {
            throw new MaterialAccessDeniedException(
                "Samo nastavno osoblje izvođenja može uklanjati spoljne veze materijala.");
        }

        var link = await _links.GetByIdAsync(command.LinkId, cancellationToken);
        if (link is null || link.MaterialId != material.Id)
            return RemoveMaterialExternalLinkOutcome.LinkNotFound;

        // Istovremeni zahtev je mogao da ukloni istu vezu između čitanja i uklanjanja.
        var removed = await _links.RemoveAsync(link.Id, cancellationToken);
        return removed
            ? RemoveMaterialExternalLinkOutcome.Removed
            : RemoveMaterialExternalLinkOutcome.LinkNotFound;
    }
}
