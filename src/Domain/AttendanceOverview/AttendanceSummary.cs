namespace Domain.AttendanceOverview;

/// <summary>
/// Zbirni pregled prisustva jednog studenta (ST-034).
/// <para>
/// Ulaz su već prebrojana stanja održanih, podobnih termina: otkazani termini i
/// termini u kojima student nije bio član ciljne grupe izbacuju se pre ove klase.
/// </para>
/// <para>
/// Procenat = Prisutan / (Prisutan + Odsutan) × 100. Stanje Neevidentirano se
/// prikazuje zasebno i ne ulazi u imenilac. Bez ijednog stanja Prisutan ili Odsutan
/// procenat ne postoji (<c>null</c>) i prikazuje se „Nema evidencije”.
/// </para>
/// </summary>
public sealed class AttendanceSummary
{
    /// <summary>Tekst koji se prikazuje umesto procenta kada nema evidentiranih stanja.</summary>
    public const string NoRecordsText = "Nema evidencije";

    // ── Svojstva ────────────────────────────────────────────────────────────

    /// <summary>Broj termina sa stanjem Prisutan.</summary>
    public int Present { get; }

    /// <summary>Broj termina sa stanjem Odsutan.</summary>
    public int Absent { get; }

    /// <summary>Broj termina za koje stanje još nije uneto (nije isto što i Odsutan).</summary>
    public int Unrecorded { get; }

    /// <summary>Ukupan broj održanih podobnih termina: Prisutan + Odsutan + Neevidentirano.</summary>
    public int HeldSessions => Present + Absent + Unrecorded;

    /// <summary>Da li postoji bar jedan zapis Prisutan ili Odsutan (osnova za procenat).</summary>
    public bool HasRecords => Present + Absent > 0;

    /// <summary>
    /// Procenat prisustva sa najviše dve decimale, ili <c>null</c> kada nema evidencije.
    /// </summary>
    public decimal? Percentage { get; }

    // ── Konstruktor ─────────────────────────────────────────────────────────

    /// <summary>
    /// Pravi pregled iz prebrojanih stanja. Negativni brojevi se odbijaju.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Ako je neki broj negativan.</exception>
    public AttendanceSummary(int present, int absent, int unrecorded)
    {
        EnsureNotNegative(present, nameof(present));
        EnsureNotNegative(absent, nameof(absent));
        EnsureNotNegative(unrecorded, nameof(unrecorded));

        Present = present;
        Absent = absent;
        Unrecorded = unrecorded;
        Percentage = CalculatePercentage(present, absent);
    }

    // ── Osnovne metode ──────────────────────────────────────────────────────

    /// <summary>
    /// Tekst za prikaz procenta: npr. „80%”, „66,67%” ili „Nema evidencije”.
    /// Koristi se zarez kao decimalni separator, kao u specifikaciji.
    /// </summary>
    public string PercentageText()
    {
        if (Percentage is null)
        {
            return NoRecordsText;
        }

        // "0.##" ispisuje najviše dve decimale i izostavlja nule na kraju (80, ne 80,00).
        var number = Percentage.Value.ToString(
            "0.##",
            System.Globalization.CultureInfo.InvariantCulture);

        return number.Replace('.', ',') + "%";
    }

    // ── Pomoćne funkcije ────────────────────────────────────────────────────

    /// <summary>
    /// P / (P + O) × 100, zaokruženo na dve decimale; polovina se zaokružuje naviše
    /// (pravilo 2.5 specifikacije). Vraća <c>null</c> kada je imenilac nula.
    /// </summary>
    private static decimal? CalculatePercentage(int present, int absent)
    {
        var denominator = present + absent;
        if (denominator == 0)
        {
            return null;
        }

        // Računa se u decimal tipu da se izbegnu greške zaokruživanja tipa double.
        var raw = (decimal)present / denominator * 100m;
        return Math.Round(raw, 2, MidpointRounding.AwayFromZero);
    }

    private static void EnsureNotNegative(int value, string paramName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                value,
                "Broj termina ne može biti negativan.");
        }
    }
}
