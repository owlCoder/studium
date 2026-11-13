namespace Application.StudentImport;

/// <summary>Исход уписа чланства и податак да ли је налог креиран.</summary>
public sealed record RowWriteResult(RowWriteOutcome Outcome, bool CreatedAccount);

/// <summary>Исход обраде једног означеног реда при потврди.</summary>
public enum RowWriteOutcome
{
    /// <summary>Налог (ако је нов) и чланство су уписани.</summary>
    Written,

    /// <summary>Чланство већ постоји; евентуални нов налог броји се одвојено.</summary>
    Duplicate,

    /// <summary>Ред се не сме обрадити; ништа није уписано.</summary>
    Error
}

/// <summary>Уговор за упис једног означеног реда.</summary>
public interface IImportRowProcessor
{
    Task<RowWriteResult> ProcessAsync(
        ParsedStudentRow row,
        Guid offeringId,
        DateTimeOffset enrolledAt,
        CancellationToken cancellationToken = default);
}

/// <summary>Уписује један ред преко портова за налоге, активацију и чланства.</summary>
/// <remarks>Стање се поново проверава пре уписа. Атомарни портови спречавају дупле уписе.</remarks>
public sealed class ImportRowProcessor : IImportRowProcessor
{
    private readonly IAccountDirectory _accounts;
    private readonly IEnrollmentDirectory _enrollments;
    private readonly IFirstActivationService _activations;

    public ImportRowProcessor(
        IAccountDirectory accounts,
        IEnrollmentDirectory enrollments,
        IFirstActivationService activations)
    {
        _accounts = accounts;
        _enrollments = enrollments;
        _activations = activations;
    }

    public async Task<RowWriteResult> ProcessAsync(
        ParsedStudentRow row,
        Guid offeringId,
        DateTimeOffset enrolledAt,
        CancellationToken cancellationToken = default)
    {
        var existing = await _accounts.FindByUsernameAsync(row.Username, cancellationToken);

        if (existing is not null)
        {
            if (!CanBeEnrolled(existing))
                return new(RowWriteOutcome.Error, false);

            // Активно чланство се не уписује поново.
            if (await _enrollments.FindActiveAsync(existing.AccountId, offeringId, cancellationToken) is not null)
                return new(RowWriteOutcome.Duplicate, false);
        }

        var createdAccount = false;
        Guid accountId;

        if (existing is null)
        {
            var registration = await RegisterAccountAsync(row, cancellationToken);
            accountId = registration.Account.AccountId;

            if (registration.Created)
            {
                // Прва активација се покреће само за налог који је овај позив креирао.
                await _activations.StartAsync(accountId, cancellationToken);
                createdAccount = true;
            }
            else if (!CanBeEnrolled(registration.Account))
            {
                // Паралелни захтев је у међувремену креирао налог који није активан студент.
                return new(RowWriteOutcome.Error, false);
            }
        }
        else
        {
            accountId = existing.AccountId;
        }

        // Атомарни упис враћа дупликат ако активно чланство већ постоји.
        var added = await _enrollments.TryAddActiveAsync(
            new NewEnrollment(accountId, offeringId, enrolledAt, EnrollmentState.Aktivno),
            cancellationToken);

        return added
            ? new(RowWriteOutcome.Written, createdAccount)
            : new(RowWriteOutcome.Duplicate, createdAccount);
    }

    private static bool CanBeEnrolled(AccountSnapshot account)
        => account.State != AccountState.Deaktiviran && account.Role == AccountRole.Student;

    private Task<AccountRegistrationResult> RegisterAccountAsync(
        ParsedStudentRow row,
        CancellationToken cancellationToken)
        => _accounts.TryAddAsync(
            new NewUserAccount(
                FirstName: row.FirstName.Trim(),
                LastName: row.LastName.Trim(),
                Username: row.Username.Trim(),
                Role: AccountRole.Student,
                State: AccountState.Aktivan,
                IsActivated: false,
                Email: string.IsNullOrWhiteSpace(row.Email) ? null : row.Email.Trim(),
                IsEmailConfirmed: false),
            cancellationToken);
}
