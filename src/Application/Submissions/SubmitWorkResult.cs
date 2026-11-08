namespace Application.Submissions;

/// <summary>
/// Ishod predaje rada (ST-012).
/// Ako je predaja uspela: Success je true, a Confirmation sadrži potvrdu.
/// Ako je predaja odbijena: Success je false, a ErrorCode kaže zašto (npr. "AssignmentNotOpen").
/// </summary>
public class SubmitWorkResult
{
    public bool Success { get; }
    public string? ErrorCode { get; }
    public SubmissionConfirmation? Confirmation { get; }

    // Konstruktor je private, pa se rezultat pravi samo preko Ok ili Fail ispod.
    private SubmitWorkResult(bool success, string? errorCode, SubmissionConfirmation? confirmation)
    {
        Success = success;
        ErrorCode = errorCode;
        Confirmation = confirmation;
    }

    // Uspešna predaja: vraćamo potvrdu, kod greške ne postoji.
    public static SubmitWorkResult Ok(SubmissionConfirmation confirmation)
    {
        return new SubmitWorkResult(true, null, confirmation);
    }

    // Odbijena predaja: vraćamo kod greške, potvrde nema.
    public static SubmitWorkResult Fail(string errorCode)
    {
        return new SubmitWorkResult(false, errorCode, null);
    }
}
