using Application.Access;
using Application.Attendance;
using Application.AttendanceOverview;
using Domain.Attendance;
using Domain.TeachingSessions;
using Infrastructure.Access;
using Infrastructure.Attendance;
using Infrastructure.AttendanceOverview;
using Moq;
using NUnit.Framework;

namespace Tests.Application;

/// <summary>
/// Testovi ličnog pregleda prisustva (ST-034, US1, US3, US4) nad memorijskim adapterima.
/// </summary>
[TestFixture]
public sealed class GetMyAttendanceHandlerTests
{
    private static readonly Guid Offering = Guid.NewGuid();
    private static readonly Guid GroupV1 = Guid.NewGuid();
    private static readonly Guid GroupV2 = Guid.NewGuid();
    private static readonly Guid Teacher = Guid.NewGuid();

    private Guid _ana;
    private Guid _marko;
    private InMemoryTeachingSessionReader _sessions = null!;
    private InMemoryEligibilityProvider _eligibility = null!;
    private InMemoryAttendanceRepository _attendance = null!;
    private DateTime _nextStart;

    [SetUp]
    public void SetUp()
    {
        _ana = Guid.NewGuid();
        _marko = Guid.NewGuid();
        _sessions = new InMemoryTeachingSessionReader();
        _attendance = new InMemoryAttendanceRepository();
        _nextStart = new DateTime(2026, 10, 5, 10, 0, 0);

        // Ana i Marko su u grupi vežbi V1; niko od njih nije u V2.
        _eligibility = new InMemoryEligibilityProvider()
            .Add(Offering, GroupV1, _ana)
            .Add(Offering, GroupV1, _marko);
    }

    // ── Pozitivni scenariji ─────────────────────────────────────────────────

    [Test]
    public async Task HandleAsync_SpecificationExample_ReturnsEightyPercent()
    {
        // Primer iz ST-034: 10 termina, 1 otkazan, 2 za grupu u kojoj Ana nije,
        // ostaje 7 podobnih: 4 Prisutan, 1 Odsutan, 2 Neevidentirano → 80%.
        await HeldSession(GroupV1, AttendanceState.Prisutan);
        await HeldSession(GroupV1, AttendanceState.Prisutan);
        await HeldSession(GroupV1, AttendanceState.Prisutan);
        await HeldSession(GroupV1, AttendanceState.Prisutan);
        await HeldSession(GroupV1, AttendanceState.Odsutan);
        await HeldSession(GroupV1, recordFor: null);
        await HeldSession(GroupV1, recordFor: null);
        CancelledSession(GroupV1);
        await HeldSession(GroupV2, AttendanceState.Prisutan, student: Guid.NewGuid());
        await HeldSession(GroupV2, AttendanceState.Prisutan, student: Guid.NewGuid());

        var result = await CreateHandler().HandleAsync(StudentAna(), Offering);
        var summary = result.Summary!;

        Assert.Multiple(() =>
        {
            Assert.That(result.IsAllowed, Is.True);
            Assert.That(result.Sessions, Has.Count.EqualTo(7));
            Assert.That(summary.Present, Is.EqualTo(4));
            Assert.That(summary.Absent, Is.EqualTo(1));
            Assert.That(summary.Unrecorded, Is.EqualTo(2));
            Assert.That(summary.PercentageText(), Is.EqualTo("80%"));
        });
    }

    [Test]
    public async Task HandleAsync_ReturnsSessionsOrderedByStart()
    {
        await HeldSession(GroupV1, AttendanceState.Prisutan);
        await HeldSession(GroupV1, AttendanceState.Odsutan);

        var result = await CreateHandler().HandleAsync(StudentAna(), Offering);

        Assert.That(result.Sessions.Select(s => s.StartsAt), Is.Ordered);
    }

    // ── Granični scenariji ──────────────────────────────────────────────────

    [Test]
    public async Task HandleAsync_IgnoresCancelledAndPlannedSessions()
    {
        await HeldSession(GroupV1, AttendanceState.Prisutan);
        CancelledSession(GroupV1);
        _sessions.Add(NewSession(GroupV1)); // ostaje Planiran

        var result = await CreateHandler().HandleAsync(StudentAna(), Offering);

        Assert.Multiple(() =>
        {
            Assert.That(result.Sessions, Has.Count.EqualTo(1));
            Assert.That(result.Summary!.HeldSessions, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task HandleAsync_OnlyUnrecordedSessions_ShowsNoRecords()
    {
        await HeldSession(GroupV1, recordFor: null);
        await HeldSession(GroupV1, recordFor: null);

        var summary = (await CreateHandler().HandleAsync(StudentAna(), Offering)).Summary!;

        Assert.Multiple(() =>
        {
            Assert.That(summary.Unrecorded, Is.EqualTo(2));
            Assert.That(summary.Percentage, Is.Null);
            Assert.That(summary.PercentageText(), Is.EqualTo("Nema evidencije"));
        });
    }

    [Test]
    public async Task HandleAsync_OtherStudentsRecords_DoNotAffectOwnOverview()
    {
        // Na istom terminu Marko je Odsutan, a za Anu nema zapisa.
        await HeldSession(GroupV1, AttendanceState.Odsutan, student: _marko);

        var result = await CreateHandler().HandleAsync(StudentAna(), Offering);

        Assert.Multiple(() =>
        {
            Assert.That(result.Sessions.Single().State, Is.EqualTo(AttendanceState.Neevidentirano));
            Assert.That(result.Summary!.Absent, Is.EqualTo(0));
        });
    }

    // ── Negativni scenariji (US4) ───────────────────────────────────────────

    [Test]
    public async Task HandleAsync_StudentNotMemberOfOffering_IsForbidden()
    {
        await HeldSession(GroupV1, AttendanceState.Prisutan);
        var attendance = new Mock<IAttendanceRepository>();
        var handler = CreateHandler(memberships: NoMembers(), attendance: attendance.Object);

        var result = await handler.HandleAsync(StudentAna(), Offering);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsAllowed, Is.False);
            Assert.That(result.Sessions, Is.Empty);
            Assert.That(result.Summary, Is.Null);
        });
        attendance.Verify(
            x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task HandleAsync_InactiveMembership_IsForbidden()
    {
        var memberships = new InMemoryMembershipReader(new[]
        {
            new OfferingMembership(_ana, Offering, MembershipKind.Student, IsActive: false)
        });

        var result = await CreateHandler(memberships: memberships)
            .HandleAsync(StudentAna(), Offering);

        Assert.That(result.IsAllowed, Is.False);
    }

    [Test]
    public async Task HandleAsync_NonStudentRole_IsForbidden()
    {
        // Lični pregled je samo za studenta; nastavnik koristi pregled grupe (US2).
        var teacher = new UserContext(Teacher, UserRole.Teacher);

        var result = await CreateHandler().HandleAsync(teacher, Offering);

        Assert.That(result.IsAllowed, Is.False);
    }

    // ── Pomoćne funkcije ────────────────────────────────────────────────────

    private GetMyAttendanceHandler CreateHandler(
        IMembershipReader? memberships = null,
        IAttendanceRepository? attendance = null) =>
        new(
            memberships ?? AnaAndMarkoAreStudents(),
            _sessions,
            _eligibility,
            attendance ?? _attendance);

    private UserContext StudentAna() => new(_ana, UserRole.Student);

    private IMembershipReader AnaAndMarkoAreStudents() =>
        new InMemoryMembershipReader(new[]
        {
            new OfferingMembership(_ana, Offering, MembershipKind.Student, IsActive: true),
            new OfferingMembership(_marko, Offering, MembershipKind.Student, IsActive: true),
            new OfferingMembership(Teacher, Offering, MembershipKind.Teacher, IsActive: true)
        });

    private static IMembershipReader NoMembers() =>
        new InMemoryMembershipReader(Array.Empty<OfferingMembership>());

    /// <summary>Novi termin u stanju Planiran, svaki sledeći dan kasnije.</summary>
    private TeachingSession NewSession(Guid? groupId)
    {
        var start = _nextStart;
        _nextStart = _nextStart.AddDays(1);
        return new TeachingSession(
            Guid.NewGuid(), "Vežbe", Offering, groupId, start, start.AddMinutes(90), "A1");
    }

    /// <summary>Održan termin; upisuje stanje za Anu (ili za zadatog studenta) ako je zadato.</summary>
    private Task HeldSession(Guid groupId, AttendanceState state, Guid? student = null) =>
        HeldSession(groupId, recordFor: (student ?? _ana, state));

    private async Task HeldSession(Guid groupId, (Guid Student, AttendanceState State)? recordFor)
    {
        var session = NewSession(groupId);
        session.MarkHeld();
        _sessions.Add(session);

        if (recordFor is { } record)
        {
            var attendance = new SessionAttendance(session.Id);
            attendance.Record(record.Student, record.State, Teacher);
            await _attendance.SaveAsync(attendance);
        }
    }

    private void CancelledSession(Guid groupId)
    {
        var session = NewSession(groupId);
        session.Cancel();
        _sessions.Add(session);
    }
}
