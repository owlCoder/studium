namespace Domain.Attendance;

/// <summary>
/// Članstvo studenta na izvođenju, u određenoj grupi ili bez grupe, sa periodom važenja.
/// Izvor ovih podataka su timovi ST-006 (upis) i ST-019 (nastavne grupe);
/// ovde se koristi samo za utvrđivanje podobnosti za prisustvo (ST-032).
/// </summary>
public sealed class GroupMembership
{
    public Guid StudentId { get; }

    public Guid OfferingId { get; }

    /// <summary>
    /// Grupa u kojoj je student, ili <c>null</c> kada je član izvođenja bez grupe.
    /// </summary>
    public Guid? GroupId { get; }

    public MembershipPeriod Period { get; }

    public GroupMembership(
        Guid studentId,
        Guid offeringId,
        Guid? groupId,
        MembershipPeriod period)
    {
        if (studentId == Guid.Empty)
            throw new ArgumentException("Student mora biti zadat.", nameof(studentId));
        if (offeringId == Guid.Empty)
            throw new ArgumentException("Izvođenje mora biti zadato.", nameof(offeringId));
        if (groupId == Guid.Empty)
            throw new ArgumentException(
                "Grupa mora biti zadata ili izostavljena.", nameof(groupId));

        StudentId = studentId;
        OfferingId = offeringId;
        GroupId = groupId;
        Period = period ?? throw new ArgumentNullException(nameof(period));
    }
}
