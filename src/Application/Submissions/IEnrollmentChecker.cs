namespace Application.Submissions;

/// <summary>
/// Proverava da li je student aktivan član izvođenja predmeta (ST-012).
/// Članstvo pravi ST-006, pa ovaj interfejs samo opisuje šta nam treba.
/// Vraća false ako student nije upisan ili mu je članstvo prekinuto.
/// </summary>
public interface IEnrollmentChecker
{
    Task<bool> IsActiveMemberAsync(Guid studentId, Guid offeringId);
}
