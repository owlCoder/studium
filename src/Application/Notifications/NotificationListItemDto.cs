using System.Globalization;
using Domain.Notifications;

namespace Application.Notifications;

/// <summary>
/// Jedna stavka u listi obaveštenja prijavljenog korisnika (ST-017, US-3).
/// Sadrži samo podatke koje primalac sme da vidi.
/// </summary>
public sealed class NotificationListItemDto
{
    private const string CreatedAtFormat = "dd.MM.yyyy. HH:mm";

    public Guid Id { get; }
    public NotificationEventType EventType { get; }
    public string Summary { get; }
    public string CreatedAt { get; }
    public bool IsRead { get; }

    public NotificationListItemDto(
        Guid id,
        NotificationEventType eventType,
        string summary,
        string createdAt,
        bool isRead)
    {
        Id = id;
        EventType = eventType;
        Summary = summary;
        CreatedAt = createdAt;
        IsRead = isRead;
    }

    public static NotificationListItemDto FromNotification(Notification notification)
    {
        ArgumentNullException.ThrowIfNull(notification);

        return new NotificationListItemDto(
            notification.Id,
            notification.EventType,
            notification.Summary,
            notification.CreatedAt.ToString(CreatedAtFormat, CultureInfo.InvariantCulture),
            notification.IsRead);
    }
}
