namespace Application.Academic;

public sealed class ChangeOfferingStateHandler
{
    private readonly ICourseOfferingRepository _offerings;

    public ChangeOfferingStateHandler(ICourseOfferingRepository offerings)
    {
        _offerings = offerings;
    }

    public async Task HandleAsync(
        ChangeOfferingStateCommand command,
        CancellationToken cancellationToken = default)
    {
        var offering = await _offerings.GetByIdAsync(command.OfferingId, cancellationToken)
            ?? throw new InvalidOperationException("Izvođenje ne postoji.");

        // Domen sam odbija nedozvoljene prelaze (npr. U pripremi -> Završeno)
        offering.ChangeState(command.NewState);

        await _offerings.UpdateAsync(offering, cancellationToken);
    }
}
