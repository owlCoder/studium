namespace Domain.Materials;

/// <summary>
/// Stanje objave nastavnog materijala (ST-009).
/// Novi materijal uvek nastaje kao nacrt; prelaze između stanja
/// (objava, povlačenje) definišu posebni zadaci.
/// </summary>
public enum MaterialPublicationState
{
    Draft = 0,
    Published = 1,
    Withdrawn = 2
}
