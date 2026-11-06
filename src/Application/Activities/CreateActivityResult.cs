namespace Application.Activities;

public enum CreateActivityStatus
{
    Created,
    CourseNotFound,
    InvalidRequest
}

public sealed record CreateActivityResult(
    CreateActivityStatus Status,
    ActivityListItem? Activity = null,
    string? Error = null)
{
    public static CreateActivityResult Created(ActivityListItem activity) =>
        new(CreateActivityStatus.Created, activity);

    public static CreateActivityResult CourseNotFound(Guid courseId) =>
        new(CreateActivityStatus.CourseNotFound,
            Error: $"Predmet sa identifikatorom {courseId} ne postoji.");

    public static CreateActivityResult InvalidRequest(string message) =>
        new(CreateActivityStatus.InvalidRequest, Error: message);
}
