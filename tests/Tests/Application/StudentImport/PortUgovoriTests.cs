using System.Text;
using Application.StudentImport;
using Infrastructure.StudentImport;
using NUnit.Framework;

namespace Tests.Application.StudentImport;

// Заједнички тестови уговора могу се применити и на друге адаптере.

public abstract class AccountDirectoryContractTests
{
    protected abstract IAccountDirectory CreateSut();

    [Test]
    public async Task Nepoznato_Ili_Prazno_Korisnicko_Ime_Vraca_Null()
    {
        var sut = CreateSut();

        Assert.That(await sut.FindByUsernameAsync("ne.postoji"), Is.Null);
        Assert.That(await sut.FindByUsernameAsync("   "), Is.Null);
    }

    [Test]
    public async Task Poredjenje_Ignorise_Velicinu_Slova_I_Unicode_Oblik()
    {
        var sut = CreateSut();
        var nfd = "đorđe.đorđević".Normalize(NormalizationForm.FormD);
        var nfcUpper = "ĐORĐE.ĐORĐEVIĆ".Normalize(NormalizationForm.FormC);

        var added = await sut.TryAddAsync(new NewUserAccount(
            FirstName: "Đorđe",
            LastName: "Đorđević",
            Username: nfd,
            Role: AccountRole.Student,
            State: AccountState.Aktivan,
            IsActivated: false,
            Email: "djordje@example.com",
            IsEmailConfirmed: false));

        Assert.That(added.Created, Is.True);

        var found = await sut.FindByUsernameAsync(nfcUpper);
        Assert.That(found, Is.Not.Null);
        Assert.That(found!.AccountId, Is.EqualTo(added.Account.AccountId));
        Assert.That(found.IsActivated, Is.False);
        Assert.That(found.IsEmailConfirmed, Is.False);
    }

    [Test]
    public async Task Atomski_TryAdd_Pri_Paralelnim_Pozivima_Kreira_Tacno_Jedan_Nalog()
    {
        var sut = CreateSut();
        using var gate = new ManualResetEventSlim(false);

        var tasks = Enumerable.Range(0, 16)
            .Select(i => Task.Run(async () =>
            {
                gate.Wait();
                var username = i % 2 == 0 ? "Ana.Petrovic" : "ana.petrovic";
                return await sut.TryAddAsync(new NewUserAccount(
                    FirstName: "Ana",
                    LastName: "Petrović",
                    Username: username,
                    Role: AccountRole.Student,
                    State: AccountState.Aktivan,
                    IsActivated: false,
                    Email: null,
                    IsEmailConfirmed: false));
            }))
            .ToArray();

        gate.Set();
        var results = await Task.WhenAll(tasks);

        Assert.That(results.Count(r => r.Created), Is.EqualTo(1));
        Assert.That(results.Select(r => r.Account.AccountId).Distinct().Count(), Is.EqualTo(1));
    }

    [Test]
    public void Novi_Nalog_Bez_Obaveznih_Podataka_Ili_Unapred_Aktiviran_Se_Odbija()
    {
        var sut = CreateSut();

        Assert.ThrowsAsync<ArgumentException>(() => sut.TryAddAsync(new NewUserAccount(
            "", "Anić", "ana.anic", AccountRole.Student, AccountState.Aktivan, false, null, false)));

        Assert.ThrowsAsync<ArgumentException>(() => sut.TryAddAsync(new NewUserAccount(
            "Ana", "", "ana.anic", AccountRole.Student, AccountState.Aktivan, false, null, false)));

        Assert.ThrowsAsync<ArgumentException>(() => sut.TryAddAsync(new NewUserAccount(
            "Ana", "Anić", "   ", AccountRole.Student, AccountState.Aktivan, false, null, false)));

        Assert.ThrowsAsync<ArgumentException>(() => sut.TryAddAsync(new NewUserAccount(
            "Ana", "Anić", "ana.anic", AccountRole.Student, AccountState.Aktivan, IsActivated: true, null, false)));
    }
}

[TestFixture]
public sealed class InMemoryAccountDirectoryContractTests : AccountDirectoryContractTests
{
    protected override IAccountDirectory CreateSut() => new InMemoryStudentAccountDirectory();

}

public abstract class EnrollmentDirectoryContractTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 10, 30, 0, TimeSpan.Zero);

    protected abstract IEnrollmentDirectory CreateSut();

    protected abstract void SeedEndedEnrollment(
        IEnrollmentDirectory sut,
        Guid accountId,
        Guid offeringId,
        DateTimeOffset enrolledAt);

    [Test]
    public async Task Prekinuto_Clanstvo_Se_Aktivira_Bez_Promene_Datuma()
    {
        var sut = CreateSut();
        var studentId = Guid.NewGuid();
        var offeringId = Guid.NewGuid();
        SeedEndedEnrollment(sut, studentId, offeringId, Now.AddDays(-30));

        Assert.That(await sut.FindActiveAsync(studentId, offeringId), Is.Null);

        var added = await sut.TryAddActiveAsync(
            new NewEnrollment(studentId, offeringId, Now, EnrollmentState.Aktivno));

        Assert.That(added, Is.True);

        var active = await sut.FindActiveAsync(studentId, offeringId);
        Assert.That(active, Is.Not.Null);
        Assert.That(active!.EnrolledAt, Is.EqualTo(Now.AddDays(-30)));
        Assert.That(active.State, Is.EqualTo(EnrollmentState.Aktivno));
    }

    [Test]
    public async Task Atomski_TryAddActive_Pri_Paralelnim_Pozivima_Upisuje_Najvise_Jedno_Aktivno_Clanstvo()
    {
        var sut = CreateSut();
        var studentId = Guid.NewGuid();
        var offeringId = Guid.NewGuid();
        using var gate = new ManualResetEventSlim(false);

        var tasks = Enumerable.Range(0, 16)
            .Select(_ => Task.Run(async () =>
            {
                gate.Wait();
                return await sut.TryAddActiveAsync(
                    new NewEnrollment(studentId, offeringId, Now, EnrollmentState.Aktivno));
            }))
            .ToArray();

        gate.Set();
        var outcomes = await Task.WhenAll(tasks);

        Assert.That(outcomes.Count(added => added), Is.EqualTo(1));
        Assert.That(outcomes.Count(added => !added), Is.EqualTo(15));
        Assert.That(await sut.FindActiveAsync(studentId, offeringId), Is.Not.Null);
    }

    [Test]
    public void Prazni_Identifikatori_Ili_Neaktivni_Novi_Upis_Se_Odbijaju()
    {
        var sut = CreateSut();

        Assert.ThrowsAsync<ArgumentException>(() => sut.TryAddActiveAsync(
            new NewEnrollment(Guid.Empty, Guid.NewGuid(), Now, EnrollmentState.Aktivno)));

        Assert.ThrowsAsync<ArgumentException>(() => sut.TryAddActiveAsync(
            new NewEnrollment(Guid.NewGuid(), Guid.Empty, Now, EnrollmentState.Aktivno)));

        Assert.ThrowsAsync<ArgumentException>(() => sut.TryAddActiveAsync(
            new NewEnrollment(Guid.NewGuid(), Guid.NewGuid(), Now, EnrollmentState.Prekinuto)));
    }
}

[TestFixture]
public sealed class InMemoryEnrollmentDirectoryContractTests : EnrollmentDirectoryContractTests
{
    protected override IEnrollmentDirectory CreateSut() => new InMemoryEnrollmentDirectory();

    protected override void SeedEndedEnrollment(
        IEnrollmentDirectory sut,
        Guid accountId,
        Guid offeringId,
        DateTimeOffset enrolledAt)
        => ((InMemoryEnrollmentDirectory)sut).Seed(
            accountId, offeringId, EnrollmentState.Prekinuto, enrolledAt);
}

[TestFixture]
public sealed class OstaliPortoviUgovorTests
{
    [Test]
    public void FixedSystemClock_Cuva_Tacan_Utc_DateTimeOffset_Iz_IClock_Ugovora()
    {
        var instant = new DateTimeOffset(2026, 11, 12, 8, 15, 0, TimeSpan.Zero);
        ISystemClock clock = new FixedSystemClock(instant);

        Assert.That(clock.UtcNow, Is.EqualTo(instant));
        Assert.That(clock.UtcNow.Offset, Is.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public void SimpleCurrentUser_Prihvata_AccountRole_Kao_AuthenticatedIdentity_Iz_ST002()
    {
        var id = Guid.NewGuid();
        ICurrentUser currentUser = new SimpleCurrentUser(id, AccountRole.Nastavnik);

        Assert.That(currentUser.AccountId, Is.EqualTo(id));
        Assert.That(AccountRoles.TryParse(currentUser.Role, out var parsed), Is.True);
        Assert.That(parsed, Is.EqualTo(AccountRole.Nastavnik));
    }
}

/// <summary>
/// Архитектонске провере се покрећу посебно: <c>dotnet test --filter
/// "FullyQualifiedName~ArhitektonskeGraniceTests"</c>.
/// </summary>
[TestFixture]
[Category("Architecture")]
[Explicit("Konvencije celog repozitorijuma: pokrenuti samo eksplicitno, ne u standardnom test batchu.")]
public sealed class ArhitektonskeGraniceTests
{
    [Test]
    public void Domain_Ne_Zavisi_Ni_Od_Jednog_Drugog_Projektnog_Sloja()
    {
        var forbidden = new[] { "Application", "Infrastructure", "Api", "ConsoleUi" };
        var referenced = typeof(global::Domain.Courses.Course).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToArray();

        Assert.That(referenced.Intersect(forbidden), Is.Empty);
    }

    [Test]
    public void Application_Ne_Zavisi_Od_Infrastrukture_Ni_Ulaznih_Slojeva()
    {
        var forbidden = new[] { "Infrastructure", "Api", "ConsoleUi" };
        var referenced = typeof(PreviewStudentImportHandler).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToArray();

        Assert.That(referenced.Intersect(forbidden), Is.Empty);
    }

    [Test]
    public void Domain_I_Application_Nemaju_Nativne_Interop_Atribute()
    {
        var assemblies = new[]
        {
            typeof(global::Domain.Courses.Course).Assembly,
            typeof(PreviewStudentImportHandler).Assembly,
        };

        var nativeMethods = assemblies
            .SelectMany(a => a.GetTypes())
            .SelectMany(t => t.GetMethods(
                System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Static
                | System.Reflection.BindingFlags.DeclaredOnly))
            .Where(m =>
                (m.Attributes & System.Reflection.MethodAttributes.PinvokeImpl) != 0
                || m.CustomAttributes.Any(ca =>
                    ca.AttributeType.Name is "DllImportAttribute" or "LibraryImportAttribute"))
            .ToArray();

        Assert.That(nativeMethods, Is.Empty);
    }
}
