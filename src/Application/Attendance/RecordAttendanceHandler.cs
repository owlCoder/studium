using Domain.Attendance;
using Domain.TeachingSessions;

namespace Application.Attendance;

/// <summary>
/// Unosi ili menja prisustvo studenta na održanom terminu (ST-032).
/// </summary>
public sealed class RecordAttendanceHandler
{
    private readonly IEligibilityProvider _eligibility;
    private readonly IAttendanceRepository _attendance;

    public RecordAttendanceHandler(
        IEligibilityProvider eligibility,
        IAttendanceRepository attendance)
    {
        _eligibility = eligibility;
        _attendance = attendance;
    }

    public async Task HandleAsync(
        TeachingSession session,
        Guid studentId,
        AttendanceState state,
        Guid actorId,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        if (session.State != SessionState.Odrzan)
            throw new InvalidOperationException(
                "Prisustvo se unosi samo za održan termin.");

        if (state == AttendanceState.Neevidentirano)
            throw new ArgumentException(
                "Evidencija mora biti Prisutan ili Odsutan.", nameof(state));

        var eligible = await _eligibility.ListEligibleStudentIdsAsync(
            session.OfferingId, session.GroupId, session.StartsAt, cancellationToken);

        if (!eligible.Contains(studentId))
            throw new InvalidOperationException("Student nije podoban za ovaj termin.");

        var attendance = await _attendance.GetAsync(session.Id, cancellationToken)
            ?? new SessionAttendance(session.Id);

        attendance.Record(studentId, state, actorId, note);

        await _attendance.SaveAsync(attendance, cancellationToken);
    }
}
