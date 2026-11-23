namespace Application.Activities;

public enum ActivityAddStatus
{
    Added,
    DuplicateId,
    InvalidActivity,
    Canceled
}

/// <summary>
/// Ishod čuvanja aktivnosti. Port ne baca izuzetke; svaki neuspeh je
/// eksplicitno naveden u <see cref="ActivityAddStatus"/>.
/// </summary>
public sealed record ActivityAddResult(ActivityAddStatus Status)
{
    public bool IsAdded => Status == ActivityAddStatus.Added;

    public static ActivityAddResult Added() =>
        new(ActivityAddStatus.Added);

    public static ActivityAddResult DuplicateId() =>
        new(ActivityAddStatus.DuplicateId);

    public static ActivityAddResult InvalidActivity() =>
        new(ActivityAddStatus.InvalidActivity);

    public static ActivityAddResult Canceled() =>
        new(ActivityAddStatus.Canceled);
}
