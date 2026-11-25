using Application.Access;
using Application.Attendance;
using Application.AttendanceOverview;
using Domain.Attendance;
using Domain.AttendanceOverview;
using Domain.TeachingSessions;
using Infrastructure.Access;
using Infrastructure.Attendance;
using Infrastructure.AttendanceOverview;
using NUnit.Framework;

namespace Tests.Application;

/// <summary>
/// Testovi pregleda prisustva grupe za nastavnika (ST-034, US2, US4).
/// </summary>
[TestFixture]
public sealed class GetGroupAttendanceHandlerTests
{
    private static readonly Guid Offering = Guid.NewGuid();
    private static readonly Guid GroupV1 = Guid.NewGuid();
    private static readonly Guid GroupV2 = Guid.NewGuid();
    private static readonly Guid Teacher = Guid.NewGuid();
    private static readonly Guid OtherTeacher = Guid.NewGuid();

    private Guid _ana;
    private Guid _marko;
    private InMemoryTeachingSessionReader _sessions = null!;
    private TimedEligibility _eligibility = null!;
    private InMemoryAttendanceRepository _attendance = null!;

    [SetUp]
    public void SetUp()
    {
        _ana = Guid.NewGuid();
        _marko = Guid.NewGuid();
        _sessions = new InMemoryTeachingSessionReader();
        _attendance = new InMemoryAttendanceRepository();

        // Ana i Marko su u V1 ceo semestar i članovi su izvođenja (predavanja za sve).
        _eligibility = new TimedEligibility()
            .Add(GroupV1, _ana).Add(GroupV1, _marko)
            .Add(null, _ana).Add(null, _marko);
    }

    // ── Pozitivni scenariji ─────────────────────────────────────────────────

    [Test]
    public async Task HandleAsync_ReturnsSummaryForEachStudentOfGroup()
    {
        await Held(GroupV1, Day(1), (_ana, AttendanceState.Prisutan), (_marko, AttendanceState.Prisutan));
        await Held(GroupV1, Day(2), (_ana, AttendanceState.Prisutan), (_marko, AttendanceState.Odsutan));

        var result = await Handler().HandleAsync(TeacherUser(), new GroupAttendanceQuery(Offering, GroupV1));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GroupAttendanceStatus.Ok));
            Assert.That(result.HeldSessions, Is.EqualTo(2));
            Assert.That(Row(result, _ana).PercentageText(), Is.EqualTo("100%"));
            Assert.That(Row(result, _marko).PercentageText(), Is.EqualTo("50%"));
        });
    }

    [Test]
    public async Task HandleAsync_PeriodFilter_IncludesBothBoundaryDays()
    {
        await Held(GroupV1, Day(1), (_ana, AttendanceState.Odsutan));
        await Held(GroupV1, Day(5), (_ana, AttendanceState.Prisutan));
        await Held(GroupV1, Day(10), (_ana, AttendanceState.Prisutan));
        await Held(GroupV1, Day(11), (_ana, AttendanceState.Odsutan));

        var query = new GroupAttendanceQuery(Offering, GroupV1,
            From: DateOnly.FromDateTime(Day(5)), To: DateOnly.FromDateTime(Day(10)));
        var result = await Handler().HandleAsync(TeacherUser(), query);

        Assert.Multiple(() =>
        {
            Assert.That(result.HeldSessions, Is.EqualTo(2));
            Assert.That(Row(result, _ana).PercentageText(), Is.EqualTo("100%"));
        });
    }

    [Test]
    public async Task HandleAsync_KindFilter_CountsOnlyThatKind()
    {
        await Held(GroupV1, Day(1), "Vežbe", (_ana, AttendanceState.Prisutan));
        await Held(null, Day(2), "Predavanja", (_ana, AttendanceState.Odsutan));

        var result = await Handler().HandleAsync(
            TeacherUser(), new GroupAttendanceQuery(Offering, Kind: "vežbe"));

        Assert.Multiple(() =>
        {
            Assert.That(result.HeldSessions, Is.EqualTo(1));
            Assert.That(Row(result, _ana).Absent, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task HandleAsync_GroupFilter_IncludesWholeOfferingSessionsButNotOtherGroups()
    {
        var petar = Guid.NewGuid();
        _eligibility.Add(GroupV2, petar).Add(null, petar);

        await Held(GroupV1, Day(1), (_ana, AttendanceState.Prisutan));
        await Held(null, Day(2), "Predavanja", (_ana, AttendanceState.Odsutan), (petar, AttendanceState.Prisutan));
        await Held(GroupV2, Day(3), (petar, AttendanceState.Prisutan));

        var result = await Handler().HandleAsync(TeacherUser(), new GroupAttendanceQuery(Offering, GroupV1));

        Assert.Multiple(() =>
        {
            Assert.That(result.HeldSessions, Is.EqualTo(2));
            Assert.That(result.Rows.Select(r => r.StudentId), Does.Not.Contain(petar));
            Assert.That(Row(result, _ana).PercentageText(), Is.EqualTo("50%"));
        });
    }

    // ── Granični scenariji ──────────────────────────────────────────────────

    [Test]
    public async Task HandleAsync_CancelledSessions_AreNotCounted()
    {
        await Held(GroupV1, Day(1), (_ana, AttendanceState.Prisutan));
        var cancelled = Session(GroupV1, Day(2));
        cancelled.Cancel();
        _sessions.Add(cancelled);

        var result = await Handler().HandleAsync(TeacherUser(), new GroupAttendanceQuery(Offering, GroupV1));

        Assert.That(result.HeldSessions, Is.EqualTo(1));
    }

    [Test]
    public async Task HandleAsync_StudentChangedGroup_CountedOnlyWhileMember()
    {
        // Marko je u V1 do 10. dana, a od tada u V2.
        var switchDay = Day(10);
        _eligibility = new TimedEligibility()
            .Add(GroupV1, _ana)
            .Add(GroupV1, _marko, until: switchDay)
            .Add(GroupV2, _marko, from: switchDay);

        await Held(GroupV1, Day(5), (_ana, AttendanceState.Prisutan), (_marko, AttendanceState.Prisutan));
        await Held(GroupV1, Day(15), (_ana, AttendanceState.Prisutan));

        var result = await Handler().HandleAsync(TeacherUser(), new GroupAttendanceQuery(Offering, GroupV1));

        Assert.Multiple(() =>
        {
            Assert.That(Row(result, _ana).HeldSessions, Is.EqualTo(2));
            Assert.That(Row(result, _marko).HeldSessions, Is.EqualTo(1));
            Assert.That(Row(result, _marko).Unrecorded, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task HandleAsync_NoHeldSessionsInPeriod_IsOkButEmpty()
    {
        await Held(GroupV1, Day(1), (_ana, AttendanceState.Prisutan));

        var query = new GroupAttendanceQuery(Offering, GroupV1,
            From: DateOnly.FromDateTime(Day(20)), To: DateOnly.FromDateTime(Day(25)));
        var result = await Handler().HandleAsync(TeacherUser(), query);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GroupAttendanceStatus.Ok));
            Assert.That(result.HasNoHeldSessions, Is.True);
            Assert.That(result.Rows, Is.Empty);
        });
    }

    // ── Negativni scenariji ─────────────────────────────────────────────────

    [Test]
    public async Task HandleAsync_TeacherNotEngagedOnOffering_IsForbidden()
    {
        await Held(GroupV1, Day(1), (_ana, AttendanceState.Prisutan));
        var other = new UserContext(OtherTeacher, UserRole.Teacher);

        var result = await Handler().HandleAsync(other, new GroupAttendanceQuery(Offering, GroupV1));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GroupAttendanceStatus.Forbidden));
            Assert.That(result.Rows, Is.Empty);
        });
    }

    [Test]
    public async Task HandleAsync_StudentRole_IsForbidden()
    {
        var student = new UserContext(_ana, UserRole.Student);

        var result = await Handler().HandleAsync(student, new GroupAttendanceQuery(Offering, GroupV1));

        Assert.That(result.Status, Is.EqualTo(GroupAttendanceStatus.Forbidden));
    }

    [Test]
    public async Task HandleAsync_PeriodEndBeforeStart_IsInvalidInput()
    {
        var query = new GroupAttendanceQuery(Offering, GroupV1,
            From: DateOnly.FromDateTime(Day(10)), To: DateOnly.FromDateTime(Day(5)));

        var result = await Handler().HandleAsync(TeacherUser(), query);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GroupAttendanceStatus.InvalidInput));
            Assert.That(result.Error, Is.Not.Null);
        });
    }

    // ── Pomoćne funkcije ────────────────────────────────────────────────────

    private GetGroupAttendanceHandler Handler() =>
        new(
            new InMemoryMembershipReader(new[]
            {
                new OfferingMembership(Teacher, Offering, MembershipKind.Teacher, IsActive: true),
                new OfferingMembership(_ana, Offering, MembershipKind.Student, IsActive: true),
                new OfferingMembership(_marko, Offering, MembershipKind.Student, IsActive: true)
            }),
            _sessions,
            _eligibility,
            _attendance);

    private static UserContext TeacherUser() => new(Teacher, UserRole.Teacher);

    private static DateTime Day(int day) => new(2026, 10, day, 10, 0, 0);

    private static TeachingSession Session(Guid? groupId, DateTime start, string kind = "Vežbe") =>
        new(Guid.NewGuid(), kind, Offering, groupId, start, start.AddMinutes(90), "A1");

    private Task Held(Guid? groupId, DateTime start, params (Guid Student, AttendanceState State)[] records) =>
        Held(groupId, start, "Vežbe", records);

    private async Task Held(
        Guid? groupId,
        DateTime start,
        string kind,
        params (Guid Student, AttendanceState State)[] records)
    {
        var session = Session(groupId, start, kind);
        session.MarkHeld();
        _sessions.Add(session);

        var attendance = new SessionAttendance(session.Id);
        foreach (var (student, state) in records)
        {
            attendance.Record(student, state, Teacher);
        }

        await _attendance.SaveAsync(attendance);
    }

    private static AttendanceSummary Row(GroupAttendanceResult result, Guid studentId) =>
        result.Rows.Single(r => r.StudentId == studentId).Summary;

    /// <summary>
    /// Zamena porta podobnosti koja poštuje vreme termina (memorijski adapter ST-032
    /// vreme ignoriše, a ovde se testira promena grupe tokom semestra).
    /// </summary>
    private sealed class TimedEligibility : IEligibilityProvider
    {
        private readonly List<(Guid? Group, Guid Student, DateTime From, DateTime Until)> _entries = new();

        public TimedEligibility Add(Guid? group, Guid student, DateTime? from = null, DateTime? until = null)
        {
            _entries.Add((group, student, from ?? DateTime.MinValue, until ?? DateTime.MaxValue));
            return this;
        }

        public Task<IReadOnlyList<Guid>> ListEligibleStudentIdsAsync(
            Guid offeringId,
            Guid? groupId,
            DateTime at,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Guid> ids = _entries
                .Where(e => e.Group == groupId && e.From <= at && at < e.Until)
                .Select(e => e.Student)
                .ToArray();
            return Task.FromResult(ids);
        }
    }
}
