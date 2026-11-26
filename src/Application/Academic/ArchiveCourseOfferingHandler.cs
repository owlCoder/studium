namespace Application.Academic;

public sealed class ArchiveCourseOfferingHandler
{
    private readonly ICourseOfferingRepository _offerings;

    public ArchiveCourseOfferingHandler(ICourseOfferingRepository offerings)
    {
        _offerings = offerings;
    }

    public async Task HandleAsync(
        ArchiveCourseOfferingCommand command,
        CancellationToken cancellationToken = default)
    {
        var offering = await _offerings.GetByIdAsync(command.OfferingId, cancellationToken)
            ?? throw new InvalidOperationException("Izvođenje ne postoji.");

        // Domen odbija arhiviranje izvođenja koje nije Završeno ili je već arhivirano
        offering.Archive();

        await _offerings.UpdateAsync(offering, cancellationToken);
    }
}
