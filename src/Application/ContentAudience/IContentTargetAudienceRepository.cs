using Domain.Entities;

namespace Application.ContentAudience;

public interface IContentTargetAudienceRepository
{
    Task AddOrUpdateAsync(ContentTargetAudience audience, CancellationToken cancellationToken);
    Task<ContentTargetAudience?> GetByContentIdAsync(Guid contentId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}