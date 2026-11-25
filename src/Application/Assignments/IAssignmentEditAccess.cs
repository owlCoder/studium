namespace Application.Assignments;

/// <summary>
/// Proverava da li prijavljeni korisnik sme da menja zadatke na izvođenju predmeta (ST-011, Izmena zadatka).
/// Vraća true samo za nastavno osoblje angažovano na tom izvođenju predmeta.
/// </summary>
public interface IAssignmentEditAccess
{
    Task<bool> CanEditAsync(Guid courseOfferingId);
}
