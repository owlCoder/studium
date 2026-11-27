using Application.TeachingSessions;
using Domain.Attendance;
using Domain.TeachingSessions;

namespace Application.Attendance;

/// <summary>
/// Vraća spisak za prisustvo sačuvanog termina (ST-032).
/// Za nepostojeći termin vraća <c>null</c>; za termin koji nije održan
/// odbija zahtev, jer se prisustvo vodi samo za održan termin.
/// Zavisi samo od portova, ne od drugih slučajeva upotrebe.
/// </summary>
public sealed class GetAttendanceSheetHandler
{
    private readonly ISessionRepository _sessions;
    private readonly IEligibilityProvider _eligibility;
    private readonly IAttendanceRepository _attendance;

    public GetAttendanceSheetHandler(
        ISessionRepository sessions,
        IEligibilityProvider eligibility,
        IAttendanceRepository attendance)
    {
        _sessions = sessions;
        _eligibility = eligibility;
        _attendance = attendance;
    }

    public async Task<AttendanceSheet?> HandleAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessions.GetByIdAsync(sessionId, cancellationToken);
        if (session is null)
            return null;

        if (session.State != SessionState.Odrzan)
            throw new InvalidOperationException(
                "Spisak podobnih se nudi samo za održan termin.");

        // Podobnost se proverava u trenutku početka termina (ADR-0005).
        var eligible = await _eligibility.ListEligibleStudentIdsAsync(
            session.OfferingId, session.GroupId, session.StartsAt, cancellationToken);

        var attendance = await _attendance.GetAsync(session.Id, cancellationToken);

        // Student bez zapisa je Neevidentirano, a ne Odsutan.
        var students = eligible
            .Select(studentId => new EligibleStudent(
                studentId,
                attendance?.StateOf(studentId) ?? AttendanceState.Neevidentirano))
            .ToArray();

        return new AttendanceSheet(session.Id, students);
    }
}
