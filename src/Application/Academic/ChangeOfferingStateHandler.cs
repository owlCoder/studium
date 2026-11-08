namespace Application.Academic;

public sealed class ChangeOfferingStateHandler
{
    private readonly ICourseOfferingRepository _offerings;

    public ChangeOfferingStateHandler(ICourseOfferingRepository offerings)
    {
        _offerings = offerings;
    }

    public Task HandleAsync(
        ChangeOfferingStateCommand command,
        CancellationToken cancellationToken = default)
    {
        // TODO: učitati izvođenje preko _offerings.GetByIdAsync (greška ako ne postoji)
        // TODO: pozvati offering.ChangeState(command.NewState) iz domena
        // TODO: sačuvati promjenu (treba nova metoda u ICourseOfferingRepository, dogovoriti sa NG)
        throw new NotImplementedException();
    }
}
