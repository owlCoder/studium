namespace Application.Materials;

/// <summary>
/// Zahtev za kreiranje nastavnog materijala kao nacrta (ST-009).
/// <paramref name="RequestingUserId"/> je identitet prijavljenog korisnika
/// koji kreira materijal; mora pripadati nastavnom osoblju izvođenja (ST-005).
/// </summary>
public sealed record CreateMaterialCommand(
    Guid RequestingUserId,
    Guid CourseOfferingId,
    string Name,
    string? Description);
