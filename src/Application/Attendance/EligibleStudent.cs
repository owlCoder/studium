using Domain.Attendance;

namespace Application.Attendance;

/// <summary>
/// Podoban student sa stanjem njegovog zapisa za dati termin.
/// </summary>
public sealed record EligibleStudent(Guid StudentId, AttendanceState State);
