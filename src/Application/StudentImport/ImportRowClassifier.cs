namespace Application.StudentImport;

/// <summary>Заједнички уговор за класификацију редова у предлогу и потврди.</summary>
public interface IImportRowClassifier
{
    Task<IReadOnlyList<ImportRowResult>> ClassifyAsync(
        IReadOnlyList<ParsedStudentRow> rows,
        Guid offeringId,
        CancellationToken cancellationToken = default);
}

/// <summary>Класификује редове применом правила и читањем података преко портова.</summary>
/// <remarks>Ред са грешком не заузима корисничко име.</remarks>
public sealed class ImportRowClassifier : IImportRowClassifier
{
    private readonly IReadOnlyList<IImportRowRule> _localRules;
    private readonly IFileScopedImportRowRule _repeatedUsername;
    private readonly IReadOnlyList<IImportRowRule> _directoryRules;
    private readonly IAccountDirectory? _accounts;

    /// <summary>Подразумевани редослед провера за увоз.</summary>
    public ImportRowClassifier(IAccountDirectory accounts, IEnrollmentDirectory enrollments)
        : this(
            accounts,
            new IImportRowRule[]
            {
                new UsernameRequiredRule(),
                new NoControlCharactersRule(),
                new EmailFormatRule(),
            },
            new RepeatedUsernameRule(),
            new IImportRowRule[]
            {
                new AccountEligibilityRule(accounts),
                new StudentNameRequiredRule(accounts),
                new ExistingEnrollmentRule(accounts, enrollments),
            })
    {
    }

    public ImportRowClassifier(
        IReadOnlyList<IImportRowRule> localRules,
        IFileScopedImportRowRule repeatedUsername,
        IReadOnlyList<IImportRowRule> directoryRules)
        : this(
            directoryRules.OfType<IAccountAwareRule>().FirstOrDefault()?.Accounts,
            localRules,
            repeatedUsername,
            directoryRules)
    {
    }

    public ImportRowClassifier(
        IAccountDirectory? accounts,
        IReadOnlyList<IImportRowRule> localRules,
        IFileScopedImportRowRule repeatedUsername,
        IReadOnlyList<IImportRowRule> directoryRules)
    {
        if (directoryRules.OfType<IAccountAwareRule>().Any(r => !ReferenceEquals(r.Accounts, accounts)))
            throw new ArgumentException("Pravila moraju koristiti isti izvor naloga.", nameof(directoryRules));

        _accounts = accounts;
        _localRules = localRules;
        _repeatedUsername = repeatedUsername;
        _directoryRules = directoryRules;
    }

    public async Task<IReadOnlyList<ImportRowResult>> ClassifyAsync(
        IReadOnlyList<ParsedStudentRow> rows,
        Guid offeringId,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ImportRowResult>(rows.Count);

        // Нормализованим поређењем препознаје еквивалентне записе имена.
        var firstSeen = new Dictionary<string, int>(UsernameComparison.Comparer);

        // Правила која читају налоге деле кеш за овај пролаз.
        IReadOnlyList<IImportRowRule> directoryRules;
        if (_accounts is not null)
        {
            var cache = new AccountLookupCache(_accounts);
            directoryRules = _directoryRules
                .Select(r => r is IAccountAwareRule aware ? aware.WithAccounts(cache) : r)
                .ToList();
        }
        else
        {
            directoryRules = _directoryRules;
        }

        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            results.Add(
                await ClassifyRowAsync(row, offeringId, firstSeen, directoryRules, cancellationToken));
        }

        return results;
    }

    private async Task<ImportRowResult> ClassifyRowAsync(
        ParsedStudentRow row,
        Guid offeringId,
        Dictionary<string, int> firstSeen,
        IReadOnlyList<IImportRowRule> directoryRules,
        CancellationToken cancellationToken)
    {
        // 1. Провере које не излазе из датотеке: одбацују неупотребљив ред.
        foreach (var rule in _localRules)
        {
            var failed = await rule.EvaluateAsync(row, offeringId, cancellationToken);
            if (failed is not null) return failed;
        }

        // 2. Понављање се проверава после локалних правила, а пре правила преко портова.
        //    Само претходно употребљив ред резервише име.
        var repeated = _repeatedUsername.Evaluate(row, firstSeen);
        if (repeated is not null) return repeated;

        // 3. Провере налога и постојећег чланства преко портова.
        foreach (var rule in directoryRules)
        {
            var failed = await rule.EvaluateAsync(row, offeringId, cancellationToken);
            if (failed is not null)
            {
                if (failed.Status == RowStatus.Duplikat)
                    firstSeen[UsernameComparison.Key(row.Username)] = row.RowNumber;
                return failed;
            }
        }

        firstSeen[UsernameComparison.Key(row.Username)] = row.RowNumber;
        return new ImportRowResult(
            row.RowNumber, row.Username, RowStatus.Ispravan, string.Empty);
    }
}

internal static class ImportClassificationContract
{
    public static void Validate(
        IReadOnlyList<ImportRowResult> classified,
        IReadOnlyDictionary<int, ParsedStudentRow> rows)
    {
        var seen = new HashSet<int>();
        foreach (var result in classified)
        {
            if (!rows.TryGetValue(result.RowNumber, out var row))
                throw new InvalidOperationException($"Klasifikator je vratio red {result.RowNumber} koji nije u ulazu.");
            if (!seen.Add(result.RowNumber)
                || !string.Equals(row.Username, result.Username, StringComparison.Ordinal)
                || !Enum.IsDefined(result.Status))
                throw new InvalidOperationException("Klasifikator je vratio neispravan rezultat reda.");
        }
        if (seen.Count != rows.Count)
            throw new InvalidOperationException("Klasifikator nije vratio sve redove.");
    }

}
