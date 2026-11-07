using Domain.Academic;

namespace Application.Academic;

public sealed class ListAcademicYearsHandler
{
    private readonly IAcademicYearRepository _repository;

    public ListAcademicYearsHandler(IAcademicYearRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<AcademicYear>> HandleAsync(
        string? query = null,
        CancellationToken cancellationToken = default)
    {
        var all = await _repository.ListAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(query))
        {
            return all;
        }

        var normalized = query.Trim();

        return all
            .Where(y => y.Label.Contains(normalized, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
