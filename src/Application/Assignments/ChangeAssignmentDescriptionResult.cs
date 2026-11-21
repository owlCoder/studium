namespace Application.Assignments;

/// <summary>
/// Ishod izmene opisa zadatka (ST-011, Izmena zadatka).
/// Ako je izmena uspela: Success je true, a Error je null.
/// Ako je izmena odbijena: Success je false, a Error kaže zašto.
/// </summary>
public class ChangeAssignmentDescriptionResult
{
    public bool Success { get; }
    public string? Error { get; }

    // Konstruktor je private, pa se rezultat pravi samo preko Ok ili Fail ispod.
    private ChangeAssignmentDescriptionResult(bool success, string? error)
    {
        Success = success;
        Error = error;
    }

    // Uspešna izmena: greške nema.
    public static ChangeAssignmentDescriptionResult Ok()
    {
        return new ChangeAssignmentDescriptionResult(true, null);
    }

    // Odbijena izmena: vraćamo poruku greške.
    public static ChangeAssignmentDescriptionResult Fail(string error)
    {
        return new ChangeAssignmentDescriptionResult(false, error);
    }
}
