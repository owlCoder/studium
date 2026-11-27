using Application.TeachingSessions;

namespace Application.Attendance;

/// <summary>
/// Vraća spisak za prisustvo sačuvanog termina (ST-032).
/// Za nepostojeći termin vraća <c>null</c>; za termin koji nije održan
/// odbija zahtev, jer se prisustvo vodi samo za održan termin.
/// </summary>
public sealed class GetAttendanceSheetHandler
{
    private readonly ISessionRepository _sessions;
    private readonly ListEligibleStudentsHandler _eligibleStudents;

    public GetAttendanceSheetHandler(
        ISessionRepository sessions,
        ListEligibleStudentsHandler eligibleStudents)
    {
        _sessions = sessions;
        _eligibleStudents = eligibleStudents;
    }

    public async Task<AttendanceSheet?> HandleAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessions.GetByIdAsync(sessionId, cancellationToken);
        if (session is null)
            return null;

        // Odbija termin koji nije održan, sa porukom iz ListEligibleStudentsHandler.
        var students = await _eligibleStudents.HandleAsync(session, cancellationToken);

        return new AttendanceSheet(session.Id, students);
    }
}
