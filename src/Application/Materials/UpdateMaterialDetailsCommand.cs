namespace Application.Materials;

/// <summary>
/// Zahtev za izmenu naziva i opisa materijala (ST-009, US-02).
/// <paramref name="RequestingUserId"/> je korisnik koji menja materijal; mora pripadati
/// nastavnom osoblju izvođenja materijala (ST-005).
/// </summary>
public sealed record UpdateMaterialDetailsCommand(
    Guid RequestingUserId,
    Guid MaterialId,
    string Name,
    string? Description);
