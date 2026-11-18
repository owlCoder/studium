namespace Domain.Notifications;

/// <summary>
/// Sistemsko obaveštenje korisniku (ST-017). Jedno obaveštenje je vezano za
/// tačno jedan poslovni događaj, označen ključem <see cref="EventKey"/>.
/// </summary>
public sealed class Notification
{
    public Guid Id { get; }
    public Guid RecipientId { get; }
    public NotificationEventType EventType { get; }
    public SourceReference Source { get; }
    public string Summary { get; }
    public string EventKey { get; }
    public DateTimeOffset CreatedAt { get; }
    public bool IsRead { get; private set; }

    private Notification(
        Guid id,
        Guid recipientId,
        NotificationEventType eventType,
        SourceReference source,
        string summary,
        string eventKey,
        DateTimeOffset createdAt)
    {
        Id = id;
        RecipientId = recipientId;
        EventType = eventType;
        Source = source;
        Summary = summary;
        EventKey = eventKey;
        CreatedAt = createdAt;
        IsRead = false;
    }

    public static Notification Create(
        Guid id,
        Guid recipientId,
        NotificationEventType eventType,
        SourceReference source,
        string summary,
        string eventKey,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Identifikator obaveštenja mora biti zadat.", nameof(id));
        if (recipientId == Guid.Empty)
            throw new ArgumentException("Primalac obaveštenja mora biti zadat.", nameof(recipientId));
        if (!Enum.IsDefined(eventType))
            throw new ArgumentOutOfRangeException(nameof(eventType), "Nepoznata vrsta događaja.");
        if (source is null)
            throw new ArgumentNullException(nameof(source), "Izvor obaveštenja mora biti zadat.");
        if (string.IsNullOrWhiteSpace(summary))
            throw new ArgumentException("Kratak opis obaveštenja je obavezan.", nameof(summary));
        if (string.IsNullOrWhiteSpace(eventKey))
            throw new ArgumentException("Ključ poslovnog događaja je obavezan.", nameof(eventKey));
        if (createdAt == default)
            throw new ArgumentException("Vreme nastanka mora biti zadato.", nameof(createdAt));

        return new Notification(
            id,
            recipientId,
            eventType,
            source,
            summary.Trim(),
            eventKey.Trim(),
            createdAt);
    }

    /// <summary>
    /// Označava obaveštenje kao pročitano (ST-017, US-4).
    /// Ponovni poziv ne menja stanje.
    /// </summary>
    public void MarkAsRead()
    {
        if (IsRead)
            return;

        IsRead = true;
    }
}
