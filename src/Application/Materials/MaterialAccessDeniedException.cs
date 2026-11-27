namespace Application.Materials;

/// <summary>
/// Korisnik nema pravo nad materijalima traženog izvođenja (ST-009, ST-005).
/// </summary>
public sealed class MaterialAccessDeniedException : Exception
{
    public MaterialAccessDeniedException(string message) : base(message) { }
}
