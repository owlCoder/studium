using Application.Access;
using Application.Attendance;
using Domain.Attendance;
using Domain.AttendanceOverview;
using Domain.TeachingSessions;

namespace Application.AttendanceOverview;

/// <summary>
/// Lični pregled prisustva studenta na jednom izvođenju (ST-034, US1, US3, US4).
/// <para>
/// Tok (DFD): 1. provera pristupa → 2. izbor podobnih održanih termina →
/// 3. čitanje stanja i računanje zbira u <see cref="AttendanceSummary"/>.
/// </para>
/// <para>
/// Handler samo čita podatke drugih timova; ne upisuje ništa ni u jedno skladište.
/// </para>
/// </summary>
public sealed class GetMyAttendanceHandler
{
    // ── Svojstva (zavisnosti preko portova) ────────────────────────────────

    private readonly IMembershipReader _memberships;        // D2: članstvo (ST-006)
    private readonly ITeachingSessionReader _sessions;      // D3: termini (ST-031)
    private readonly IEligibilityProvider _eligibility;     // D2: grupa u vreme termina (ST-006/019)
    private readonly IAttendanceRepository _attendance;     // D4: zapisi prisustva (ST-032)

    // ── Konstruktor ─────────────────────────────────────────────────────────

    public GetMyAttendanceHandler(
        IMembershipReader memberships,
        ITeachingSessionReader sessions,
        IEligibilityProvider eligibility,
        IAttendanceRepository attendance)
    {
        _memberships = memberships;
        _sessions = sessions;
        _eligibility = eligibility;
        _attendance = attendance;
    }

    // ── Osnovna metoda ──────────────────────────────────────────────────────

    /// <summary>
    /// Vraća pregled za prijavljenog studenta. Identitet se uzima iz
    /// <paramref name="user"/> (prijava), a ne iz parametra koji šalje klijent,
    /// pa student ne može tražiti tuđi pregled (US4, N1).
    /// </summary>
    public async Task<MyAttendanceResult> HandleAsync(
        UserContext user,
        Guid offeringId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        // 1. Provera pristupa: samo student sa aktivnim članstvom na ovom izvođenju.
        if (!await CanViewAsync(user, offeringId, cancellationToken))
        {
            return MyAttendanceResult.Forbidden();
        }

        // 2. Izbor termina: samo održani (otkazani i planirani se ne računaju)
        //    i samo oni u kojima je student bio u ciljnoj grupi u vreme termina.
        var allSessions = await _sessions.ListByOfferingAsync(offeringId, cancellationToken);
        var items = new List<SessionAttendanceItem>();

        foreach (var session in allSessions
                     .Where(s => s.State == SessionState.Odrzan)
                     .OrderBy(s => s.StartsAt))
        {
            if (!await WasEligibleAsync(user.UserId, session, cancellationToken))
            {
                continue;
            }

            // 3. Stanje: bez zapisa je Neevidentirano (ne Odsutan).
            var state = await StateOfAsync(user.UserId, session.Id, cancellationToken);
            items.Add(new SessionAttendanceItem(session.Id, session.Kind, session.StartsAt, state));
        }

        return MyAttendanceResult.Allowed(items, Summarize(items));
    }

    // ── Pomoćne funkcije ────────────────────────────────────────────────────

    /// <summary>Lični pregled je samo za studente koji su aktivni članovi izvođenja.</summary>
    private async Task<bool> CanViewAsync(
        UserContext user,
        Guid offeringId,
        CancellationToken cancellationToken)
    {
        if (user.Role != UserRole.Student)
        {
            return false;
        }

        return await _memberships.IsActiveStudentAsync(user.UserId, offeringId, cancellationToken);
    }

    /// <summary>Da li je student bio podoban za termin (članstvo i grupa u vreme termina).</summary>
    private async Task<bool> WasEligibleAsync(
        Guid studentId,
        TeachingSession session,
        CancellationToken cancellationToken)
    {
        var eligible = await _eligibility.ListEligibleStudentIdsAsync(
            session.OfferingId,
            session.GroupId,
            session.StartsAt,
            cancellationToken);

        return eligible.Contains(studentId);
    }

    /// <summary>Stanje studenta na terminu; Neevidentirano ako evidencija ne postoji.</summary>
    private async Task<AttendanceState> StateOfAsync(
        Guid studentId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var attendance = await _attendance.GetAsync(sessionId, cancellationToken);
        return attendance?.StateOf(studentId) ?? AttendanceState.Neevidentirano;
    }

    /// <summary>Prebrojava stanja i predaje brojeve kalkulatoru iz Domain sloja.</summary>
    private static AttendanceSummary Summarize(IReadOnlyCollection<SessionAttendanceItem> items) =>
        new(
            present: items.Count(i => i.State == AttendanceState.Prisutan),
            absent: items.Count(i => i.State == AttendanceState.Odsutan),
            unrecorded: items.Count(i => i.State == AttendanceState.Neevidentirano));
}
