using Domain.Attendance;
using Domain.TeachingSessions;

namespace Application.Attendance;

/// <summary>
/// Izlistava studente podobne za održan termin i razdvaja unete od Neevidentiranih.
/// </summary>
public sealed class ListEligibleStudentsHandler
{
    private readonly IEligibilityProvider _eligibility;
    private readonly IAttendanceRepository _attendance;

    public ListEligibleStudentsHandler(
        IEligibilityProvider eligibility,
        IAttendanceRepository attendance)
    {
        _eligibility = eligibility;
        _attendance = attendance;
    }

    public async Task<IReadOnlyList<EligibleStudent>> HandleAsync(
        TeachingSession session,
        CancellationToken cancellationToken = default)
    {
       if (session.State != SessionState.Odrzan)
            throw new InvalidOperationException(
                "Spisak podobnih se nudi samo za održan termin.");

        var eligible = await _eligibility.ListEligibleStudentIdsAsync(
            session.OfferingId, session.GroupId, session.StartsAt, cancellationToken);

        var attendance = await _attendance.GetAsync(session.Id, cancellationToken);

        return eligible
            .Select(studentId => new EligibleStudent(
                studentId,
                attendance?.StateOf(studentId) ?? AttendanceState.Neevidentirano))
            .ToArray();
    }
}
