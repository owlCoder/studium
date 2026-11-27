namespace Domain.Attendance;

/// <summary>
/// Pravilo podobnosti za prisustvo (ST-032): "Podobnost se utvrđuje prema
/// članstvu i grupi u vreme termina."
/// </summary>
public static class EligibilityRule
{
    /// <summary>
    /// Vraća studente koji su u trenutku <paramref name="at"/> bili članovi
    /// ciljne grupe termina. Kada je <paramref name="groupId"/> <c>null</c>,
    /// termin je namenjen celom izvođenju i obuhvata članove svih grupa.
    /// Svaki student se vraća najviše jednom, redom kojim se prvi put pojavi.
    /// </summary>
    public static IReadOnlyList<Guid> EligibleStudents(
        IEnumerable<GroupMembership> memberships,
        Guid offeringId,
        Guid? groupId,
        DateTime at)
    {
        ArgumentNullException.ThrowIfNull(memberships);

        return memberships
            .Where(m => m.OfferingId == offeringId)
            .Where(m => groupId is null || m.GroupId == groupId)
            .Where(m => m.Period.Includes(at))
            .Select(m => m.StudentId)
            .Distinct()
            .ToArray();
    }
}
