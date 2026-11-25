using Domain.Attendance;
using Domain.AttendanceOverview;

namespace Application.AttendanceOverview;

/// <summary>
/// Jedan održan, podoban termin u ličnom pregledu: kada je bio, koja je vrsta
/// nastave i koje je stanje prisustva studenta.
/// </summary>
public sealed record SessionAttendanceItem(
    Guid SessionId,
    string Kind,
    DateTime StartsAt,
    AttendanceState State);

/// <summary>
/// Ishod zahteva za lični pregled prisustva (ST-034, US1).
/// <para>
/// Kada pristup nije dozvoljen, <see cref="IsAllowed"/> je <c>false</c> i ne vraća se
/// nijedan podatak — ni termini ni brojevi (US4).
/// </para>
/// </summary>
public sealed class MyAttendanceResult
{
    // ── Svojstva ────────────────────────────────────────────────────────────

    /// <summary>Poruka za odbijen zahtev (pravilo 2.6: korisniku se saopštava razlog).</summary>
    public const string ForbiddenMessage = "Nedovoljno ovlašćenje.";

    /// <summary>Da li je korisnik smeo da dobije pregled.</summary>
    public bool IsAllowed { get; }

    /// <summary>Održani podobni termini, od najstarijeg; prazno kada pristup nije dozvoljen.</summary>
    public IReadOnlyList<SessionAttendanceItem> Sessions { get; }

    /// <summary>Brojevi i procenat; <c>null</c> kada pristup nije dozvoljen.</summary>
    public AttendanceSummary? Summary { get; }

    // ── Konstruktor ─────────────────────────────────────────────────────────

    private MyAttendanceResult(
        bool isAllowed,
        IReadOnlyList<SessionAttendanceItem> sessions,
        AttendanceSummary? summary)
    {
        IsAllowed = isAllowed;
        Sessions = sessions;
        Summary = summary;
    }

    // ── Osnovne metode (fabrike) ────────────────────────────────────────────

    /// <summary>Uspešan pregled sa terminima i zbirom.</summary>
    public static MyAttendanceResult Allowed(
        IReadOnlyList<SessionAttendanceItem> sessions,
        AttendanceSummary summary) =>
        new(true, sessions, summary);

    /// <summary>Odbijen zahtev: bez ikakvih podataka o prisustvu.</summary>
    public static MyAttendanceResult Forbidden() =>
        new(false, Array.Empty<SessionAttendanceItem>(), null);
}
