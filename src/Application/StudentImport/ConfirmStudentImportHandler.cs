namespace Application.StudentImport;

/// <summary>Потврђује означене исправне редове без дуплирања налога и чланстава.</summary>
/// <remarks>Бројачи обухватају означене редове; упис је издвојен у <see cref="IImportRowProcessor"/>.</remarks>
public sealed class ConfirmStudentImportHandler
{
    private readonly IStudentImportAuthorizer _authorizer;
    private readonly IStudentOfferingGuard _offering;
    private readonly IStudentListParser _parser;
    private readonly IStudentListFileReader _fileReader;
    private readonly IImportRowClassifier _classifier;
    private readonly IImportRowProcessor _processor;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISystemClock _clock;

    public ConfirmStudentImportHandler(
        IStudentImportAuthorizer authorizer,
        IStudentOfferingGuard offering,
        IStudentListParser parser,
        IImportRowClassifier classifier,
        IImportRowProcessor processor,
        IUnitOfWork unitOfWork,
        ISystemClock clock)
        : this(
            authorizer, offering, parser, classifier, processor,
            unitOfWork, clock, new StudentListFileReader())
    {
    }

    public ConfirmStudentImportHandler(
        IStudentImportAuthorizer authorizer,
        IStudentOfferingGuard offering,
        IStudentListParser parser,
        IImportRowClassifier classifier,
        IImportRowProcessor processor,
        IUnitOfWork unitOfWork,
        ISystemClock clock,
        IStudentListFileReader fileReader)
    {
        _authorizer = authorizer;
        _offering = offering;
        _parser = parser;
        _classifier = classifier;
        _processor = processor;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _fileReader = fileReader;
    }

    /// <summary>Улаз из датотеке; права и извођење проверавају се пре читања бајтова.</summary>
    public Task<ImportConfirmationResult> HandleAsync(
        Guid offeringId,
        Stream stream,
        CancellationToken cancellationToken = default)
        => HandleAsync(offeringId, stream, selectedRows: null, cancellationToken);

    /// <summary>Улаз из датотеке уз избор означених редова.</summary>
    public async Task<ImportConfirmationResult> HandleAsync(
        Guid offeringId,
        Stream stream,
        IReadOnlyCollection<int>? selectedRows,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeAndVerifyOfferingAsync(offeringId, cancellationToken);

        var text = await _fileReader.ReadAsync(stream, cancellationToken);
        return await ExecuteImportAsync(offeringId, text, selectedRows, cancellationToken);
    }

    /// <summary>Поново проверава улаз и обрађује све исправне редове.</summary>
    public Task<ImportConfirmationResult> HandleAsync(
        Guid offeringId,
        string csvText,
        CancellationToken cancellationToken = default)
        => HandleAsync(offeringId, csvText, selectedRows: null, cancellationToken);

    /// <summary>
    /// Поново проверава улаз и обрађује означене исправне редове. <paramref name="selectedRows"/> садржи
    /// физичке бројеве редова; <c>null</c> означава све редове, а празна колекција ниједан.
    /// </summary>
    public async Task<ImportConfirmationResult> HandleAsync(
        Guid offeringId,
        string csvText,
        IReadOnlyCollection<int>? selectedRows,
        CancellationToken cancellationToken = default)
    {
        // Права се проверавају поново, независно од претходног предлога.
        await AuthorizeAndVerifyOfferingAsync(offeringId, cancellationToken);
        return await ExecuteImportAsync(offeringId, csvText, selectedRows, cancellationToken);
    }

    private async Task AuthorizeAndVerifyOfferingAsync(
        Guid offeringId,
        CancellationToken cancellationToken)
    {
        await _authorizer.EnsureCanImportAsync(offeringId, cancellationToken);
        await _offering.EnsureExistsAsync(offeringId, cancellationToken);
    }

    private Task<ImportConfirmationResult> ExecuteImportAsync(
        Guid offeringId,
        string csvText,
        IReadOnlyCollection<int>? selectedRows,
        CancellationToken cancellationToken)
    {
        var parsed = _parser.Parse(csvText);

        // Речник и скуп омогућавају брзу претрагу означених редова.
        var rowsByNumber = parsed.Rows.ToDictionary(r => r.RowNumber);
        var selected = selectedRows is null ? null : new HashSet<int>(selectedRows);

        // Сви редови овог увоза користе исто време уписа.
        var enrolledAt = _clock.UtcNow;

        // Класификација и упис припадају истој трансакцији.
        return _unitOfWork.ExecuteAsync(
            async token =>
            {
                // Класификују се сви редови, јер дупликати важе за целу датотеку.
                var classified = await _classifier.ClassifyAsync(
                    parsed.Rows, offeringId, token);

                ImportClassificationContract.Validate(classified, rowsByNumber);
                return await WriteSelectedRowsAsync(
                    classified, rowsByNumber, selected, offeringId, enrolledAt, parsed.Warnings, token);
            },
            cancellationToken);
    }

    private async Task<ImportConfirmationResult> WriteSelectedRowsAsync(
        IReadOnlyList<ImportRowResult> classified,
        IReadOnlyDictionary<int, ParsedStudentRow> rowsByNumber,
        HashSet<int>? selected,
        Guid offeringId,
        DateTimeOffset enrolledAt,
        IReadOnlyList<string> warnings,
        CancellationToken cancellationToken)
    {
        var createdAccounts = 0;
        var createdMemberships = 0;
        var duplicates = 0;
        var errors = 0;

        foreach (var result in classified)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Неозначени редови не улазе у бројаче; null означава све редове.
            if (selected is not null && !selected.Contains(result.RowNumber)) continue;

            if (result.Status == RowStatus.Duplikat) { duplicates++; continue; }
            if (result.Status != RowStatus.Ispravan) { errors++; continue; }

            var row = rowsByNumber[result.RowNumber];

            // Процесор поново проверава стање; изузетак поништава трансакцију.
            var write = await _processor.ProcessAsync(
                row, offeringId, enrolledAt, cancellationToken);

            switch (write.Outcome)
            {
                case RowWriteOutcome.Written:
                    createdMemberships++;
                    if (write.CreatedAccount) createdAccounts++;
                    break;

                case RowWriteOutcome.Duplicate:
                    duplicates++;
                    // Нов налог се броји и ако је чланство у међувремену уписао други позив.
                    if (write.CreatedAccount) createdAccounts++;
                    break;

                default:
                    errors++;
                    break;
            }
        }

        return new ImportConfirmationResult(
            createdAccounts,
            createdMemberships,
            duplicates,
            errors,
            warnings);
    }
}
