namespace Application.StudentImport;

// Локални портови одвајају увоз од спољних сервиса и складишта.

// ── Налози ────────────────────────────────────────────────────────────────

/// <summary>Читање и упис корисничких налога.</summary>
public interface IAccountDirectory
{
    /// <summary>
    /// Проналази налог по корисничком имену, без разлике у величини слова. Враћа <c>null</c> ако налог не
    /// постоји.
    /// </summary>
    Task<AccountSnapshot?> FindByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default);

    /// <summary>Атомарно враћа постојећи налог или креира нови, без дуплих корисничких имена.</summary>
    Task<AccountRegistrationResult> TryAddAsync(
        NewUserAccount account,
        CancellationToken cancellationToken = default);
}

/// <summary>Резултат провере и уписа налога.</summary>
public sealed record AccountRegistrationResult(
    AccountSnapshot Account,
    bool Created);

/// <summary>Подаци постојећег корисничког налога.</summary>
public sealed record AccountSnapshot(
    Guid AccountId,
    string FirstName,
    string LastName,
    string Username,
    AccountRole Role,
    AccountState State,
    bool IsActivated,
    string? Email,
    bool IsEmailConfirmed);

/// <summary>Нови студентски налог је активан, али чека прву активацију. Унета адреса није потврђена.</summary>
public sealed record NewUserAccount(
    string FirstName,
    string LastName,
    string Username,
    AccountRole Role,
    AccountState State,
    bool IsActivated,
    string? Email,
    bool IsEmailConfirmed);

// ── Чланства ─────────────────────────────────────────────────────────────

/// <summary>Читање и упис чланстава.</summary>
public interface IEnrollmentDirectory
{
    /// <summary>
    /// Враћа активно чланство за пар студента и извођења, или <c>null</c>. Прекинуто чланство није активно
    /// и може се поново активирати.
    /// </summary>
    Task<EnrollmentSnapshot?> FindActiveAsync(
        Guid accountId,
        Guid offeringId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Атомарно активира постојеће чланство или уписује ново, без дуплирања записа. Поновна активација чува
    /// идентификатор и првобитни датум уписа.
    /// </summary>
    /// <returns><c>true</c> ако је чланство активирано или уписано, <c>false</c> ако је већ активно.</returns>
    Task<bool> TryAddActiveAsync(
        NewEnrollment enrollment,
        CancellationToken cancellationToken = default);
}

/// <summary>Стање чланства.</summary>
public enum EnrollmentState
{
    /// <summary>Чланство важи.</summary>
    Aktivno,

    /// <summary>Запис остаје, чланство не важи.</summary>
    Prekinuto,
}

/// <summary>Постојеће чланство у пуном облику.</summary>
public sealed record EnrollmentSnapshot(
    Guid EnrollmentId,
    Guid AccountId,
    Guid OfferingId,
    DateTimeOffset EnrolledAt,
    EnrollmentState State);

/// <summary>Подаци за упис новог чланства.</summary>
public sealed record NewEnrollment(
    Guid AccountId,
    Guid OfferingId,
    DateTimeOffset EnrolledAt,
    EnrollmentState State);

// ── Извођења предмета ───────────────────────────────────────────────────

/// <summary>Читање извођења предмета.</summary>
public interface IOfferingDirectory
{
    Task<OfferingSnapshot?> FindAsync(Guid offeringId, CancellationToken cancellationToken = default);
}

// ── Идентитет и ангажовања ───────────────────────────────────────

/// <summary>Идентитет који даје спољни слој; увоз не спроводи аутентификацију.</summary>
public interface ICurrentUser
{
    /// <summary>Идентификатор налога, или <c>null</c> ако корисник није пријављен.</summary>
    Guid? AccountId { get; }

    /// <summary>Улога корисника: Администратор, Наставник, Асистент или Студент.</summary>
    string? Role { get; }
}

/// <summary>Одговорност наставника на извођењу. Само одговорни наставник има право увоза.</summary>
public interface ITeachingAssignmentDirectory
{
    Task<bool> IsResponsibleTeacherAsync(
        Guid staffAccountId,
        Guid offeringId,
        CancellationToken cancellationToken = default);
}

// ── Права на увоз ─────────────────────────────────────────────────────────────────

/// <summary>Провера права над конкретним извођењем у апликационом слоју.</summary>
public interface IStudentImportAuthorizer
{
    Task EnsureCanImportAsync(Guid offeringId, CancellationToken cancellationToken = default);
}

// ── Прва активација ───────────────────────────────────────────────

/// <summary>Покретање прве активације преко порта, без одређивања токена и канала.</summary>
public interface IFirstActivationService
{
    Task StartAsync(Guid accountId, CancellationToken cancellationToken = default);
}

// ── Време и трансакције ───────────────────────────────────────────────────────────

/// <summary>Извор системског времена за датум уписа чланства.</summary>
public interface ISystemClock
{
    DateTimeOffset UtcNow { get; }
}

/// <summary>Извршава упис као целину; неуспех поништава измене тог посла.</summary>
public interface IUnitOfWork
{
    Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> work,
        CancellationToken cancellationToken = default);
}
