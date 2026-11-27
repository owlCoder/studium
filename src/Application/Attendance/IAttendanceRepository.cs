using Domain.Attendance;

namespace Application.Attendance;

/// <summary>
/// Port za čuvanje evidencije prisustva po terminu.
/// </summary>
public interface IAttendanceRepository
{
    Task<SessionAttendance?> GetAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Vraća postojeću evidenciju termina ili napravi novu, tako da za jedan
    /// termin uvek postoji tačno jedna evidencija, i kada dva unosa stignu istovremeno.
    /// </summary>
    Task<SessionAttendance> GetOrCreateAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        SessionAttendance attendance,
        CancellationToken cancellationToken = default);
}
