using Application.Academic;
using Domain.Academic;

namespace Infrastructure.Academic;

public sealed class InMemoryAcademicYearRepository : IAcademicYearRepository
{
    private readonly object _lock = new();
    private readonly List<AcademicYear> _years = new();

    public Task<bool> ExistsByLabelAsync(string label, CancellationToken cancellationToken = default)
    {
        var normalized = label.Trim();
        var exists = _years.Any(y =>
            string.Equals(y.Label, normalized, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(exists);
    }

    public Task<AcademicYear?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_years.FirstOrDefault(y => y.Id == id));
    }

    public Task AddAsync(AcademicYear year, CancellationToken cancellationToken = default)
    {
        _years.Add(year);
        return Task.CompletedTask;
    }

    public Task<bool> TryAddAsync(AcademicYear year, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_years.Any(y => string.Equals(y.Label, year.Label, StringComparison.OrdinalIgnoreCase)))
            {
                return Task.FromResult(false);
            }

            _years.Add(year);
            return Task.FromResult(true);
        }
    }
    public Task<IReadOnlyList<AcademicYear>> ListAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            IReadOnlyList<AcademicYear> result = _years.OrderBy(y => y.StartsAt).ToList();
            return Task.FromResult(result);
        }
    }

    // Vraca false ako godina ne postoji ili je oznaka zauzeta drugom godinom.
    public Task<bool> TryUpdateAsync(AcademicYear year, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var index = _years.FindIndex(y => y.Id == year.Id);
            if (index < 0)
            {
                return Task.FromResult(false);
            }

            var labelTaken = _years.Any(y =>
                y.Id != year.Id &&
                string.Equals(y.Label, year.Label, StringComparison.OrdinalIgnoreCase));

            if (labelTaken)
            {
                return Task.FromResult(false);
            }

            _years[index] = year;
            return Task.FromResult(true);
        }
    }
}
