namespace Application.Materials;

/// <summary>
/// Zahtev za kreiranje nastavnog materijala kao nacrta (ST-009).
/// </summary>
public sealed record CreateMaterialCommand(
    Guid CourseOfferingId,
    string Name,
    string? Description);
