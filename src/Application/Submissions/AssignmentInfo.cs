namespace Application.Submissions;

/// <summary>
/// Podaci o zadatku koji su potrebni za predaju rada (ST-012).
/// </summary>
public class AssignmentInfo
{
    public Guid OfferingId { get; }
    public AssignmentState State { get; }
    public DateTimeOffset Deadline { get; }

    public AssignmentInfo(Guid offeringId, AssignmentState state, DateTimeOffset deadline)
    {
        OfferingId = offeringId;
        State = state;
        Deadline = deadline;
    }
}
