namespace Application.Materials;

/// <summary>
/// Zahtev za izmenu naziva i opisa materijala (ST-009, US-02).
/// </summary>
public sealed record UpdateMaterialDetailsCommand(
    Guid MaterialId,
    string Name,
    string? Description);
