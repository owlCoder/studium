using Domain.Attendance;

namespace Application.Attendance;

/// <summary>
/// Spisak za prisustvo jednog održanog termina (ST-032): podobni studenti sa
/// trenutnim stanjem, uz broj unetih i broj neevidentiranih.
/// Neevidentiran student nema zapis i ne računa se kao odsutan.
/// </summary>
public sealed record AttendanceSheet(Guid SessionId, IReadOnlyList<EligibleStudent> Students)
{
    /// <summary>Broj podobnih studenata.</summary>
    public int EligibleCount => Students.Count;

    /// <summary>Broj studenata za koje je uneto Prisutan ili Odsutan.</summary>
    public int EnteredCount => Students.Count(s => s.State != AttendanceState.Neevidentirano);

    /// <summary>Broj studenata bez unosa.</summary>
    public int UnrecordedCount => Students.Count(s => s.State == AttendanceState.Neevidentirano);
}
