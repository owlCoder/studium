
namespace Application.Assignments;

public sealed record CreateAssignmentCommand(
    Guid CourseOfferingId,
    string? Title,
    string? Description,
    string? Instructions,
    DateTimeOffset? Deadline
);
