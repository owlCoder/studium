namespace Application.Notifications;

/// <summary>
/// Lista obaveštenja prijavljenog korisnika, od najnovijeg ka starijem (ST-017, US-3).
/// </summary>
public sealed class ListMyNotificationsHandler
{
    private readonly INotificationReader _reader;

    public ListMyNotificationsHandler(INotificationReader reader)
    {
        _reader = reader;
    }

    public async Task<IReadOnlyList<NotificationListItemDto>> HandleAsync(
        Guid recipientId,
        CancellationToken cancellationToken = default)
    {
        if (recipientId == Guid.Empty)
            throw new ArgumentException("Primalac mora biti zadat.", nameof(recipientId));

        var notifications = await _reader.ListForRecipientAsync(recipientId, cancellationToken);

        return notifications
            .Where(n => n.RecipientId == recipientId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(NotificationListItemDto.FromNotification)
            .ToArray();
    }
}
