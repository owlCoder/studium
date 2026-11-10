namespace Domain.Attendance;

/// <summary>
/// Zapis prisustva za jednog studenta na jednom terminu (ST-032).
/// Nastaje tek prvim unosom, uvek kao Prisutan ili Odsutan.
/// </summary>
public sealed class AttendanceRecord
{
    public Guid Id { get; }
    public Guid SessionId { get; }
    public Guid StudentId { get; }
    public AttendanceState State { get; private set; }
    public Guid ActorId { get; private set; }
    public string Note { get; private set; }

    public AttendanceRecord(
        Guid id,
        Guid sessionId,
        Guid studentId,
        AttendanceState state,
        Guid actorId,
        string? note = null)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Identifikator mora biti zadat.", nameof(id));
        if (sessionId == Guid.Empty)
            throw new ArgumentException("Termin mora biti zadat.", nameof(sessionId));
        if (studentId == Guid.Empty)
            throw new ArgumentException("Student mora biti zadat.", nameof(studentId));
        if (actorId == Guid.Empty)
            throw new ArgumentException("Izvršilac mora biti zadat.", nameof(actorId));
        if (state == AttendanceState.Neevidentirano)
            throw new ArgumentException(
                "Zapis ne može nastati u stanju Neevidentirano.", nameof(state));

        Id = id;
        SessionId = sessionId;
        StudentId = studentId;
        State = state;
        ActorId = actorId;
        Note = note?.Trim() ?? string.Empty;
    }

    public void Record(AttendanceState state, Guid actorId, string? note = null)
    {
        if (state == AttendanceState.Neevidentirano)
            throw new ArgumentException(
                "Evidencija mora biti Prisutan ili Odsutan.", nameof(state));
        if (actorId == Guid.Empty)
            throw new ArgumentException("Izvršilac mora biti zadat.", nameof(actorId));

        State = state;
        ActorId = actorId;
        Note = note?.Trim() ?? string.Empty;
    }
}
