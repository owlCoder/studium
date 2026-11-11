using Application.Materials;

namespace Infrastructure.Materials;

/// <summary>
/// Privremeni memorijski adapter za proveru nastavnog osoblja izvođenja
/// dok se ne integriše ST-005. Bez zadatih angažovanja odbija sve korisnike
/// (podrazumevano zabranjeno). Zameniti adapterom nad servisom tima ST-005.
/// </summary>
public sealed class InMemoryOfferingStaffChecker : IOfferingStaffChecker
{
    private readonly HashSet<(Guid UserId, Guid OfferingId)> _assignments;

    public InMemoryOfferingStaffChecker(
        IEnumerable<(Guid UserId, Guid OfferingId)>? assignments = null)
    {
        _assignments = new HashSet<(Guid, Guid)>(assignments ?? []);
    }

    public Task<bool> IsStaffOnOfferingAsync(
        Guid userId,
        Guid offeringId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_assignments.Contains((userId, offeringId)));
    }
}
