using Application.Staff;
using Domain.Staff;

namespace Infrastructure.Staff;

public sealed class InMemoryTeachingAssignmentRepository : ITeachingAssignmentRepository
{
    private readonly List<TeachingAssignment> _items = new();
    private readonly object _sync = new();

    public Task<TeachingAssignment?> GetActiveAsync(
        Guid offeringId, Guid userId, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult(_items.FirstOrDefault(
                a => a.OfferingId == offeringId && a.UserId == userId && a.IsActive));
        }
    }

    public Task<IReadOnlyList<TeachingAssignment>> ListActiveByOfferingAsync(
        Guid offeringId, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult<IReadOnlyList<TeachingAssignment>>(
                _items.Where(a => a.OfferingId == offeringId && a.IsActive).ToList());
        }
    }

    public Task AddAsync(TeachingAssignment assignment, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            bool duplicate = _items.Any(a =>
                a.OfferingId == assignment.OfferingId &&
                a.UserId == assignment.UserId &&
                a.IsActive);

            if (duplicate)
            {
                throw new InvalidOperationException("Aktivno angažovanje već postoji.");
            }

            _items.Add(assignment);
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(TeachingAssignment assignment, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (!_items.Any(a => a.Id == assignment.Id))
            {
                throw new InvalidOperationException("Angažovanje ne postoji.");
            }
        }

        return Task.CompletedTask;
    }
}
