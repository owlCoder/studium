using Application.StudentImport;
using Infrastructure.StudentImport;

namespace Tests.Application.StudentImport;

/// <summary>
/// Заједничка поставка: меморијски адаптери, администратор, одговорни наставник и једно активно извођење.
/// </summary>
internal sealed class StudentImportFixture
{
    public static readonly DateTimeOffset Now = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

    public Guid OfferingId { get; } = Guid.NewGuid();
    public Guid CourseId { get; } = Guid.NewGuid();
    public Guid AcademicYearId { get; } = Guid.NewGuid();
    public Guid AdminId { get; } = Guid.NewGuid();

    /// <summary>Одговорни наставник на <see cref="OfferingId"/> — сме да увози.</summary>
    public Guid TeacherId { get; } = Guid.NewGuid();

    /// <summary>Наставник без ангажовања на <see cref="OfferingId"/> — не сме да увози.</summary>
    public Guid OtherTeacherId { get; } = Guid.NewGuid();

    public Guid StudentId { get; } = Guid.NewGuid();

    public InMemoryStudentAccountDirectory Accounts { get; } = new();
    public InMemoryFirstActivationService Activations { get; } = new();
    public InMemoryEnrollmentDirectory Enrollments { get; } = new();
    public InMemoryOfferingDirectory Offerings { get; } = new();
    public InMemoryTeachingAssignmentDirectory Assignments { get; } = new();
    public IStudentListFileReader FileReader { get; } = new StudentListFileReader();
    public StudentListCsvParser Parser { get; } = new();
    public FixedSystemClock Clock { get; } = new(Now);
    public IUnitOfWork UnitOfWork { get; }

    public StudentImportFixture()
    {
        UnitOfWork = new InMemoryUnitOfWork(Accounts, Activations, Enrollments);

        Offerings.Seed(new OfferingSnapshot(
            OfferingId,
            CourseId,
            AcademicYearId,
            OfferingState.Aktivno,
            IsArchived: false));

        Assignments.Seed(TeacherId, OfferingId, TeachingResponsibility.OdgovorniNastavnik);
    }

    public IImportRowClassifier Classifier => new ImportRowClassifier(Accounts, Enrollments);

    public IStudentOfferingGuard OfferingGuard => new StudentOfferingGuard(Offerings);

    public IStudentImportAuthorizer AuthorizerFor(Guid? accountId, string? role)
        => new StudentImportAuthorizer(new SimpleCurrentUser(accountId, role), Assignments);

    /// <summary>Пример из спецификације: 20 редова, 2 дупликата, 1 ред без корисничког имена.</summary>
    public const string SpecExample =
        "firstname,lastname,username,email\n" +
        "Ana,Anić,ana.anic,ana@example.com\n" +
        "Marko,Marković,marko.markovic,\n" +
        "Jelena,Jelić,jelena.jelic,jelena@example.com\n" +
        "Nikola,Nikolić,nikola.nikolic,nikola@example.com\n" +
        "Milica,Milić,milica.milic,milica@example.com\n" +
        "Stefan,Stefanović,stefan.stefanovic,stefan@example.com\n" +
        "Ivana,Ivanović,ivana.ivanovic,ivana@example.com\n" +
        "Petar,Petrović,petar.petrovic,petar@example.com\n" +
        "Jovana,Jovanović,jovana.jovanovic,jovana@example.com\n" +
        "Luka,Lukić,luka.lukic,luka@example.com\n" +
        "Marija,Marić,marija.maric,marija@example.com\n" +
        "Đorđe,Đorđević,djordje.djordjevic,djordje@example.com\n" +
        "Sara,Sarić,sara.saric,sara@example.com\n" +
        "Vuk,Vukić,vuk.vukic,vuk@example.com\n" +
        "Tijana,Tijanić,tijana.tijanic,tijana@example.com\n" +
        "Filip,Filipović,filip.filipovic,filip@example.com\n" +
        "Nina,Ninić,nina.ninic,nina@example.com\n" +
        "Ana,Anić,ana.anic,ana@example.com\n" +
        "Marko,Marković,marko.markovic,\n" +
        "Bez,Imena,,bezimena@example.com\n";
}
