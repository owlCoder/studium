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
}
