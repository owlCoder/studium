namespace Application.Attendance;

/// <summary>
/// Port ka članstvu i grupama (ST-006). Vraća studente podobne za cilj termina
/// u vreme održavanja. Implementaciju obezbeđuje drugi tim.
/// </summary>
public interface IEligibilityProvider
{
    Task<IReadOnlyList<Guid>> ListEligibleStudentIdsAsync(
        Guid offeringId,
        Guid? groupId,
        DateTime at,
        CancellationToken cancellationToken = default);
}
