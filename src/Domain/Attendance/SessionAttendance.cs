namespace Domain.Attendance;

/// <summary>
/// Evidencija prisustva za jedan termin. Za par (student, termin) postoji tačno
/// jedan zapis; ponovljeni unos menja isti zapis, a student bez unosa je Neevidentiran.
/// </summary>
public sealed class SessionAttendance
{
    private readonly Guid _sessionId;
    private readonly Dictionary<Guid, AttendanceRecord> _records = new();

    public SessionAttendance(Guid sessionId)
    {
        if (sessionId == Guid.Empty)
            throw new ArgumentException("Termin mora biti zadat.", nameof(sessionId));

        _sessionId = sessionId;
    }

    public Guid SessionId => _sessionId;

    public IReadOnlyList<AttendanceRecord> Records => _records.Values.ToArray();

    public AttendanceRecord Record(
        Guid studentId,
        AttendanceState state,
        Guid actorId,
        string? note = null)
    {
        if (_records.TryGetValue(studentId, out var existing))
        {
            existing.Record(state, actorId, note);
            return existing;
        }

        var record = new AttendanceRecord(
            Guid.NewGuid(), _sessionId, studentId, state, actorId, note);
        _records[studentId] = record;
        return record;
    }

    public AttendanceState StateOf(Guid studentId) =>
        _records.TryGetValue(studentId, out var record)
            ? record.State
            : AttendanceState.Neevidentirano;
}
