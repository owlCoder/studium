namespace Application.Activities;

public enum CreateActivityStatus
{
    Created,
    ContextNotFound,
    InvalidRequest,
    Conflict,
    Canceled
}

public sealed record CreateActivityResult(
    CreateActivityStatus Status,
    ActivityListItem? Activity = null,
    string? Error = null)
{
    public static CreateActivityResult Created(ActivityListItem activity) =>
        new(CreateActivityStatus.Created, activity);

    public static CreateActivityResult ContextNotFound(string contextSlug) =>
        new(CreateActivityStatus.ContextNotFound,
            Error: $"Kontekst aktivnosti sa oznakom \"{contextSlug}\" ne postoji.");

    public static CreateActivityResult InvalidRequest(string message) =>
        new(CreateActivityStatus.InvalidRequest, Error: message);

    public static CreateActivityResult Conflict(string message) =>
        new(CreateActivityStatus.Conflict, Error: message);

    public static CreateActivityResult Canceled() =>
        new(CreateActivityStatus.Canceled,
            Error: "Zahtev je otkazan pre čuvanja aktivnosti.");
}
