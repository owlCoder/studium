namespace Application.StudentImport;

/// <summary>Приказује предлог по реду и не мења ниједан запис.</summary>
public sealed class PreviewStudentImportHandler
{
    private readonly IStudentImportAuthorizer _authorizer;
    private readonly IStudentOfferingGuard _offering;
    private readonly IStudentListParser _parser;
    private readonly IStudentListFileReader _fileReader;
    private readonly IImportRowClassifier _classifier;

    public PreviewStudentImportHandler(
        IStudentImportAuthorizer authorizer,
        IStudentOfferingGuard offering,
        IStudentListParser parser,
        IImportRowClassifier classifier)
        : this(authorizer, offering, parser, classifier, new StudentListFileReader())
    {
    }

    public PreviewStudentImportHandler(
        IStudentImportAuthorizer authorizer,
        IStudentOfferingGuard offering,
        IStudentListParser parser,
        IImportRowClassifier classifier,
        IStudentListFileReader fileReader)
    {
        _authorizer = authorizer;
        _offering = offering;
        _parser = parser;
        _classifier = classifier;
        _fileReader = fileReader;
    }

    /// <summary>Проверава права и извођење пре читања датотеке.</summary>
    public async Task<ImportPreview> HandleAsync(
        Guid offeringId,
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeAndVerifyOfferingAsync(offeringId, cancellationToken);

        var text = await _fileReader.ReadAsync(stream, cancellationToken);
        return await PreviewFromTextAsync(offeringId, text, cancellationToken);
    }

    public async Task<ImportPreview> HandleAsync(
        Guid offeringId,
        string csvText,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeAndVerifyOfferingAsync(offeringId, cancellationToken);
        return await PreviewFromTextAsync(offeringId, csvText, cancellationToken);
    }

    private async Task AuthorizeAndVerifyOfferingAsync(
        Guid offeringId,
        CancellationToken cancellationToken)
    {
        await _authorizer.EnsureCanImportAsync(offeringId, cancellationToken);

        // Увоз се везује за конкретно извођење предмета, не за каталог предмета.
        await _offering.EnsureExistsAsync(offeringId, cancellationToken);
    }

    private async Task<ImportPreview> PreviewFromTextAsync(
        Guid offeringId,
        string csvText,
        CancellationToken cancellationToken)
    {
        var parsed = _parser.Parse(csvText);
        var rows = await _classifier.ClassifyAsync(parsed.Rows, offeringId, cancellationToken);

        ImportClassificationContract.Validate(rows, parsed.Rows.ToDictionary(r => r.RowNumber));

        // Збирови се не попуњавају овде: изведени су из редова у самом предлогу.
        return new ImportPreview(offeringId, rows, parsed.Warnings);
    }
}
