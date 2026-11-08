namespace Studium.Application.Reviews;

using Studium.Domain.Reviews;

public interface IReviewRepository
{
    Task<ReviewDraft?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateAsync(ReviewDraft draft, CancellationToken cancellationToken = default);
}