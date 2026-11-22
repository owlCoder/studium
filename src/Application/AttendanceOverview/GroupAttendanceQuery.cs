namespace Application.AttendanceOverview;

/// <summary>
/// Filteri pregleda prisustva grupe (ST-034, US2). Prazan filter znači „bez ograničenja”.
/// </summary>
/// <param name="OfferingId">Izvođenje predmeta (obavezno).</param>
/// <param name="GroupId">Nastavna grupa; <c>null</c> = svi studenti izvođenja.</param>
/// <param name="From">Prvi dan perioda, uključivo; <c>null</c> = od početka.</param>
/// <param name="To">Poslednji dan perioda, uključivo; <c>null</c> = do kraja.</param>
/// <param name="Kind">Vrsta nastave (npr. „Vežbe”); <c>null</c> = sve vrste.</param>
public sealed record GroupAttendanceQuery(
    Guid OfferingId,
    Guid? GroupId = null,
    DateOnly? From = null,
    DateOnly? To = null,
    string? Kind = null);
