namespace Application.Activities;

/// <summary>
/// Rezultat razrešavanja oznake konteksta iz URL-a.
/// </summary>
public sealed record ActivityContextLookup(
    ActivityContextStatus Status,
    ActivityContext? Context,
    string? Error)
{
    public bool IsSuccess => Status == ActivityContextStatus.Found;

    public static ActivityContextLookup Found(ActivityContext context) =>
        new(ActivityContextStatus.Found, context, null);

    public static ActivityContextLookup NotFound(string error) =>
        new(ActivityContextStatus.NotFound, null, error);

    public static ActivityContextLookup Ambiguous(string error) =>
        new(ActivityContextStatus.Ambiguous, null, error);

    public static ActivityContextLookup Canceled() =>
        new(ActivityContextStatus.Canceled, null, "Zahtev je otkazan.");
}
