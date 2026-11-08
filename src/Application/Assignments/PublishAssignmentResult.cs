namespace Application.Assignments;

public sealed record PublishAssignmentResult(bool IsSuccess, string? Error)
{
    public static PublishAssignmentResult Success() => new(true, null);

    public static PublishAssignmentResult Failure(string error) => new(false, error);
}
