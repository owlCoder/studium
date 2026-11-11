namespace Application.StudentImport;

/// <summary>Провера реда која се може додати без измене класификатора.</summary>
public interface IImportRowRule
{
    Task<ImportRowResult?> EvaluateAsync(
        ParsedStudentRow row,
        Guid offeringId,
        CancellationToken cancellationToken);
}

/// <summary>Правило које користи налоге; <see cref="WithAccounts"/> омогућава увођење кеша.</summary>
public interface IAccountAwareRule : IImportRowRule
{
    IAccountDirectory Accounts { get; }

    IImportRowRule WithAccounts(IAccountDirectory accounts);
}

/// <summary>Провера која користи стање целе датотеке, нпр. већ виђена корисничка имена.</summary>
public interface IFileScopedImportRowRule
{
    ImportRowResult? Evaluate(
        ParsedStudentRow row,
        IReadOnlyDictionary<string, int> firstSeenRowByUsername);
}

/// <summary>Проверава да ли је корисничко име унето.</summary>
public sealed class UsernameRequiredRule : IImportRowRule
{
    public Task<ImportRowResult?> EvaluateAsync(
        ParsedStudentRow row, Guid offeringId, CancellationToken cancellationToken)
        => Task.FromResult(row.UsernameIsBlank()
            ? ImportRowResults.Error(row, "Jedinstveno korisnicko ime je obavezno.")
            : null);
}

/// <summary>Име и презиме су обавезни за нов налог, али не и за упис постојећег налога.</summary>
public sealed class StudentNameRequiredRule : IAccountAwareRule
{
    private readonly IAccountDirectory _accounts;

    public StudentNameRequiredRule(IAccountDirectory accounts) => _accounts = accounts;

    public IAccountDirectory Accounts => _accounts;

    public IImportRowRule WithAccounts(IAccountDirectory accounts)
        => new StudentNameRequiredRule(accounts);

    public async Task<ImportRowResult?> EvaluateAsync(
        ParsedStudentRow row, Guid offeringId, CancellationToken cancellationToken)
    {
        var account = await _accounts.FindByUsernameAsync(row.Username, cancellationToken);

        // Постојећи налог има своје име; увоз га не мења, па му име у реду није потребно.
        if (account is not null) return null;

        return string.IsNullOrWhiteSpace(row.FirstName) || string.IsNullOrWhiteSpace(row.LastName)
            ? ImportRowResults.Error(row, "Ime i prezime su obavezni za novi nalog.")
            : null;
    }
}

/// <summary>Адреса је опциона; када је дата, мора бити употребљивог облика.</summary>
public sealed class EmailFormatRule : IImportRowRule
{
    public Task<ImportRowResult?> EvaluateAsync(
        ParsedStudentRow row, Guid offeringId, CancellationToken cancellationToken)
        => Task.FromResult(ImportRowResults.HasInvalidEmail(row)
            ? ImportRowResults.Error(row, "Neispravan format adrese elektronske poste.")
            : null);
}

/// <summary>Одбија управљачке знакове који могу покварити запис у складишту.</summary>
public sealed class NoControlCharactersRule : IImportRowRule
{
    public Task<ImportRowResult?> EvaluateAsync(
        ParsedStudentRow row, Guid offeringId, CancellationToken cancellationToken)
        => Task.FromResult(
            HasControlCharacter(row.Username)
            || HasControlCharacter(row.FirstName)
            || HasControlCharacter(row.LastName)
            || HasControlCharacter(row.Email)
                ? ImportRowResults.Error(row, "Polje sadrzi nedozvoljen upravljacki znak.")
                : null);

    private static bool HasControlCharacter(string? value)
        => !string.IsNullOrEmpty(value) && value.Any(char.IsControl);
}

/// <summary>Препознаје поновљено употребљиво корисничко име у датотеци.</summary>
public sealed class RepeatedUsernameRule : IFileScopedImportRowRule
{
    public ImportRowResult? Evaluate(
        ParsedStudentRow row,
        IReadOnlyDictionary<string, int> firstSeenRowByUsername)
        => firstSeenRowByUsername.TryGetValue(UsernameComparison.Key(row.Username), out var firstRow)
            ? new ImportRowResult(
                row.RowNumber, row.Username, RowStatus.Duplikat,
                $"Korisnicko ime se ponavlja u datoteci; prvi put u redu {firstRow}.")
            : null;
}

/// <summary>Провера подобности постојећег налога: уписују се само активни студентски налози.</summary>
/// <remarks>Налог који чека прву активацију сме добити чланство; активација је услов за пријаву.</remarks>
public sealed class AccountEligibilityRule : IAccountAwareRule
{
    private readonly IAccountDirectory _accounts;

    public AccountEligibilityRule(IAccountDirectory accounts) => _accounts = accounts;

    public IAccountDirectory Accounts => _accounts;

    public IImportRowRule WithAccounts(IAccountDirectory accounts)
        => new AccountEligibilityRule(accounts);

    public async Task<ImportRowResult?> EvaluateAsync(
        ParsedStudentRow row, Guid offeringId, CancellationToken cancellationToken)
    {
        var account = await _accounts.FindByUsernameAsync(row.Username, cancellationToken);
        if (account is null) return null;

        if (account.State == AccountState.Deaktiviran)
            return ImportRowResults.Error(row, "Nalog je deaktiviran i ne moze biti upisan.");

        if (account.Role != AccountRole.Student)
            return ImportRowResults.Error(row, "Nalog nema ulogu studenta i ne moze biti upisan.");

        return null;
    }
}

/// <summary>Проверава да ли студент већ има активно чланство на извођењу.</summary>
/// <remarks>Прекинуто чланство може се поново активирати без новог записа.</remarks>
public sealed class ExistingEnrollmentRule : IAccountAwareRule
{
    private readonly IAccountDirectory _accounts;
    private readonly IEnrollmentDirectory _enrollments;

    public ExistingEnrollmentRule(IAccountDirectory accounts, IEnrollmentDirectory enrollments)
    {
        _accounts = accounts;
        _enrollments = enrollments;
    }

    public IAccountDirectory Accounts => _accounts;

    public IImportRowRule WithAccounts(IAccountDirectory accounts)
        => new ExistingEnrollmentRule(accounts, _enrollments);

    public async Task<ImportRowResult?> EvaluateAsync(
        ParsedStudentRow row, Guid offeringId, CancellationToken cancellationToken)
    {
        var account = await _accounts.FindByUsernameAsync(row.Username, cancellationToken);

        if (account is null) return null;

        var active = await _enrollments.FindActiveAsync(account.AccountId, offeringId, cancellationToken);

        return active is not null
            ? new ImportRowResult(
                row.RowNumber, row.Username, RowStatus.Duplikat,
                "Nalog je vec clan ovog izvodjenja predmeta.")
            : null;
    }
}

/// <summary>Поређење корисничких имена у NFC облику, без разлике у величини слова.</summary>
/// <remarks>Парсер уклања околни празан простор и примењује NFC; чува величину слова и дијакритике.</remarks>
public static class UsernameComparison
{
    public static readonly StringComparer Comparer = new NfcComparer();

    public static string Key(string username) => username.Normalize().Trim();

    /// <summary>Своди канонски еквивалентне записе на NFC, без промене величине слова.</summary>
    public static string Canonical(string username)
        => string.IsNullOrEmpty(username) ? username : username.Normalize();

    private sealed class NfcComparer : StringComparer
    {
        public override int Compare(string? x, string? y)
            => OrdinalIgnoreCase.Compare(x?.Normalize(), y?.Normalize());

        public override bool Equals(string? x, string? y)
            => OrdinalIgnoreCase.Equals(x?.Normalize(), y?.Normalize());

        public override int GetHashCode(string value)
            => OrdinalIgnoreCase.GetHashCode(value.Normalize());
    }
}

internal static class ImportRowResults
{
    public static ImportRowResult Error(ParsedStudentRow row, string reason)
        => new(row.RowNumber, row.Username, RowStatus.Greska, reason);

    public static bool HasInvalidEmail(ParsedStudentRow row)
        => !string.IsNullOrWhiteSpace(row.Email) && !EmailPattern.IsMatch(row.Email);

    private static readonly System.Text.RegularExpressions.Regex EmailPattern = new(
        @"^[^@\s]+@(?:[^@\s.]+\.)+[^@\s.]+$",
        System.Text.RegularExpressions.RegexOptions.Compiled
        | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
}

internal static class ParsedStudentRowExtensions
{
    public static bool UsernameIsBlank(this ParsedStudentRow row)
        => string.IsNullOrWhiteSpace(row.Username);
}
