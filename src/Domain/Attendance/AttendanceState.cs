namespace Domain.Attendance;

/// <summary>
/// Stanje zapisa prisustva (ST-032). <see cref="Neevidentirano"/> znači da zapis
/// za tog studenta još ne postoji i ne tumači se kao odsustvo.
/// </summary>
public enum AttendanceState
{
    Neevidentirano,
    Prisutan,
    Odsutan
}
