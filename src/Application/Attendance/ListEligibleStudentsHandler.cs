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
        // dodati funkcionalnost
    }
}
