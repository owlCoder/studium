using Application;
using Application.Groups;

namespace Infrastructure.Groups;

/// <summary>
/// Stvarna provera postojanja grupe nad portom <see cref="IGroupRepository"/>.
/// Grupa postoji samo ako je repozitorijum vraca; nema podrazumevanog odgovora "true".
/// </summary>
public sealed class RepositoryGroupLookup : IGroupLookup
{
    private readonly IGroupRepository _groupRepository;

    public RepositoryGroupLookup(IGroupRepository groupRepository)
    {
        _groupRepository = groupRepository;
    }

    public async Task<bool> ExistsAsync(Guid groupId, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetByIdAsync(groupId, cancellationToken);
        return group != null;
    }

    public Task<Domain.Entities.Group?> GetByIdAsync(Guid groupId, CancellationToken cancellationToken)
    {
        return _groupRepository.GetByIdAsync(groupId, cancellationToken);
    }
}
