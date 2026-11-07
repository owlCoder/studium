using Domain.Academic;

namespace Application.Academic;

public sealed class CreateAcademicYearHandler
{
    private readonly IAcademicYearRepository _repository;

    public CreateAcademicYearHandler(IAcademicYearRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> HandleAsync(
        CreateAcademicYearCommand command,
        CancellationToken cancellationToken = default)
    {
        var year = new AcademicYear(
            Guid.NewGuid(),
            command.Label,
            command.StartsAt,
            command.EndsAt);

        if (await _repository.ExistsByLabelAsync(year.Label, cancellationToken))
        {
            throw new InvalidOperationException(
                $"Akademska godina '{year.Label}' već postoji.");
        }

        await _repository.AddAsync(year, cancellationToken);
        return year.Id;
    }
}
