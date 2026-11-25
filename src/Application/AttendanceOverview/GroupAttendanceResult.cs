using Domain.AttendanceOverview;

namespace Application.AttendanceOverview;

/// <summary>Ishod zahteva za pregled grupe (pravilo 2.6: razlog neuspeha se saopštava).</summary>
public enum GroupAttendanceStatus
{
    /// <summary>Pregled je napravljen (može biti i bez održanih termina).</summary>
    Ok,

    /// <summary>Korisnik nije nastavno osoblje angažovano na izvođenju.</summary>
    Forbidden,

    /// <summary>Neispravan unos, npr. kraj perioda pre početka.</summary>
    InvalidInput
}

/// <summary>Jedan red pregleda: student i njegov zbir prisustva za izabrane filtere.</summary>
public sealed record StudentAttendanceRow(Guid StudentId, AttendanceSummary Summary);

/// <summary>
/// Pregled prisustva grupe za nastavnika (ST-034, US2).
/// </summary>
public sealed class GroupAttendanceResult
{
    // ── Svojstva ────────────────────────────────────────────────────────────

    /// <summary>Poruka kada u izabranom periodu nema održanih termina (US2, G3).</summary>
    public const string NoHeldSessionsMessage = "Nema održanih termina";

    public GroupAttendanceStatus Status { get; }

    /// <summary>Razlog kada <see cref="Status"/> nije <see cref="GroupAttendanceStatus.Ok"/>.</summary>
    public string? Error { get; }

    /// <summary>Broj održanih termina koji odgovaraju filterima.</summary>
    public int HeldSessions { get; }

    /// <summary>Red po studentu; prazno kada nema održanih termina ili pristup nije dozvoljen.</summary>
    public IReadOnlyList<StudentAttendanceRow> Rows { get; }

    /// <summary>Uspešan pregled, ali bez ijednog održanog termina — razlikuje se od greške.</summary>
    public bool HasNoHeldSessions => Status == GroupAttendanceStatus.Ok && HeldSessions == 0;

    // ── Konstruktor ─────────────────────────────────────────────────────────

    private GroupAttendanceResult(
        GroupAttendanceStatus status,
        string? error,
        int heldSessions,
        IReadOnlyList<StudentAttendanceRow> rows)
    {
        Status = status;
        Error = error;
        HeldSessions = heldSessions;
        Rows = rows;
    }

    // ── Osnovne metode (fabrike) ────────────────────────────────────────────

    public static GroupAttendanceResult Ok(int heldSessions, IReadOnlyList<StudentAttendanceRow> rows) =>
        new(GroupAttendanceStatus.Ok, null, heldSessions, rows);

    public static GroupAttendanceResult Forbidden() =>
        new(GroupAttendanceStatus.Forbidden, MyAttendanceResult.ForbiddenMessage, 0,
            Array.Empty<StudentAttendanceRow>());

    public static GroupAttendanceResult InvalidInput(string reason) =>
        new(GroupAttendanceStatus.InvalidInput, reason, 0, Array.Empty<StudentAttendanceRow>());
}
