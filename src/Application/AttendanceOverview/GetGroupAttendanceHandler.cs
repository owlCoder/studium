using Application.Access;
using Application.Attendance;
using Domain.Attendance;
using Domain.AttendanceOverview;
using Domain.TeachingSessions;

namespace Application.AttendanceOverview;

/// <summary>
/// Pregled prisustva grupe za nastavno osoblje (ST-034, US2, US4).
/// <para>
/// Tok (DFD): 1. provera unosa i pristupa → 2. izbor održanih termina po filterima →
/// 3. za svaki termin: podobni studenti i njihova stanja → zbir po studentu.
/// </para>
/// <para>
/// Student se računa samo za termine u kojima je bio podoban u vreme termina
/// (članstvo i grupa, ST-034 i ST-019), pa promena grupe ne menja ranije termine.
/// </para>
/// </summary>
public sealed class GetGroupAttendanceHandler
{
    // ── Svojstva (zavisnosti preko portova) ────────────────────────────────

    private readonly IMembershipReader _memberships;        // D2: angažovanje (ST-005)
    private readonly ITeachingSessionReader _sessions;      // D3: termini (ST-031)
    private readonly IEligibilityProvider _eligibility;     // D2: grupa u vreme termina (ST-006/019)
    private readonly IAttendanceRepository _attendance;     // D4: zapisi prisustva (ST-032)

    // ── Konstruktor ─────────────────────────────────────────────────────────

    public GetGroupAttendanceHandler(
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

    public async Task<GroupAttendanceResult> HandleAsync(
        UserContext user,
        GroupAttendanceQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(query);

        // 1a. Provera unosa (US2, N2): ne čita se nikakav podatak.
        if (query.From is { } from && query.To is { } to && to < from)
        {
            return GroupAttendanceResult.InvalidInput("Kraj perioda ne može biti pre početka.");
        }

        // 1b. Provera pristupa (US2, N1): nastavnik ili asistent angažovan na izvođenju.
        if (!await CanViewAsync(user, query.OfferingId, cancellationToken))
        {
            return GroupAttendanceResult.Forbidden();
        }

        // 2. Održani termini koji odgovaraju svim filterima (otkazani i planirani ispadaju).
        var allSessions = await _sessions.ListByOfferingAsync(query.OfferingId, cancellationToken);
        var held = allSessions
            .Where(s => s.State == SessionState.Odrzan)
            .Where(s => MatchesFilters(s, query))
            .ToArray();

        // 3. Za svaki termin: ko je bio podoban i sa kojim stanjem.
        var statesByStudent = new Dictionary<Guid, List<AttendanceState>>();

        foreach (var session in held)
        {
            var students = await EligibleStudentsAsync(session, query.GroupId, cancellationToken);
            var attendance = await _attendance.GetAsync(session.Id, cancellationToken);

            foreach (var studentId in students)
            {
                var state = attendance?.StateOf(studentId) ?? AttendanceState.Neevidentirano;

                if (!statesByStudent.TryGetValue(studentId, out var states))
                {
                    states = new List<AttendanceState>();
                    statesByStudent[studentId] = states;
                }

                states.Add(state);
            }
        }

        var rows = statesByStudent
            .OrderBy(pair => pair.Key)
            .Select(pair => new StudentAttendanceRow(pair.Key, Summarize(pair.Value)))
            .ToArray();

        return GroupAttendanceResult.Ok(held.Length, rows);
    }

    // ── Pomoćne funkcije ────────────────────────────────────────────────────

    /// <summary>Pregled grupe vide samo nastavnik i asistent angažovani na izvođenju.</summary>
    private async Task<bool> CanViewAsync(
        UserContext user,
        Guid offeringId,
        CancellationToken cancellationToken)
    {
        if (user.Role is not (UserRole.Teacher or UserRole.Assistant))
        {
            return false;
        }

        return await _memberships.IsEngagedStaffAsync(user.UserId, offeringId, cancellationToken);
    }

    /// <summary>
    /// Filter grupe propušta termine te grupe i termine za celo izvođenje (npr. predavanja);
    /// termini drugih grupa ispadaju. Period je po datumu početka, obe granice uključivo.
    /// </summary>
    private static bool MatchesFilters(TeachingSession session, GroupAttendanceQuery query)
    {
        if (query.GroupId is { } groupId && session.GroupId is not null && session.GroupId != groupId)
        {
            return false;
        }

        var date = DateOnly.FromDateTime(session.StartsAt);
        if (query.From is { } from && date < from)
        {
            return false;
        }

        if (query.To is { } to && date > to)
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(query.Kind)
               || string.Equals(session.Kind, query.Kind.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Studenti podobni za termin u vreme termina. Kada je izabrana grupa, zadržavaju se
    /// samo oni koji su u tom trenutku bili članovi te grupe.
    /// </summary>
    private async Task<IReadOnlyCollection<Guid>> EligibleStudentsAsync(
        TeachingSession session,
        Guid? selectedGroup,
        CancellationToken cancellationToken)
    {
        var forSession = await _eligibility.ListEligibleStudentIdsAsync(
            session.OfferingId, session.GroupId, session.StartsAt, cancellationToken);

        if (selectedGroup is null || session.GroupId == selectedGroup)
        {
            return forSession.ToHashSet();
        }

        // Termin za celo izvođenje, a izabrana je grupa: presek sa članovima grupe tada.
        var groupMembers = await _eligibility.ListEligibleStudentIdsAsync(
            session.OfferingId, selectedGroup, session.StartsAt, cancellationToken);

        return forSession.Intersect(groupMembers).ToHashSet();
    }

    private static AttendanceSummary Summarize(IReadOnlyCollection<AttendanceState> states) =>
        new(
            present: states.Count(s => s == AttendanceState.Prisutan),
            absent: states.Count(s => s == AttendanceState.Odsutan),
            unrecorded: states.Count(s => s == AttendanceState.Neevidentirano));
}
