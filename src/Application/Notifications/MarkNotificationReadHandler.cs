namespace Application.Notifications;

/// <summary>
/// Označava obaveštenje prijavljenog korisnika kao pročitano (ST-017, US-4).
/// Tuđe obaveštenje dobija isti odgovor kao nepostojeće, da odgovor ne otkrije da postoji.
/// </summary>
public sealed class MarkNotificationReadHandler
{
    private readonly INotificationReader _reader;
    private readonly INotificationWriter _writer;

    public MarkNotificationReadHandler(INotificationReader reader, INotificationWriter writer)
    {
        _reader = reader;
        _writer = writer;
    }

    public async Task<MarkNotificationReadResult> HandleAsync(
        Guid notificationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var notification = await _reader.GetByIdAsync(notificationId, cancellationToken);
        if (notification is null || notification.RecipientId != userId)
            return MarkNotificationReadResult.NotFound;

        if (notification.IsRead)
            return MarkNotificationReadResult.Marked;

        notification.MarkAsRead();
        await _writer.SaveAsync(notification, cancellationToken);
        return MarkNotificationReadResult.Marked;
    }
}
