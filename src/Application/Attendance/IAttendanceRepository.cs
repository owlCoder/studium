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

    Task SaveAsync(
        SessionAttendance attendance,
        CancellationToken cancellationToken = default);
}
