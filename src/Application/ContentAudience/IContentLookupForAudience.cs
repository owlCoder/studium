namespace Application.ContentAudience;

public interface IContentLookupForAudience
{
    Task<bool> ExistsAsync(Guid contentId, CancellationToken cancellationToken);
    Task<Guid> GetCourseExecutionIdAsync(Guid contentId, CancellationToken cancellationToken);
    Task<bool> CanInstructorEditContentAsync(Guid contentId, Guid instructorId, CancellationToken cancellationToken);
}