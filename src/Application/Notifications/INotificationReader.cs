namespace Application.Notifications;

/// <summary>
/// Čitanje obaveštenja iz skladišta D1 (ST-017, US-2).
/// </summary>
public interface INotificationReader
{
    /// <summary>
    /// Proverava da li za primaoca već postoji obaveštenje o istom poslovnom događaju.
    /// </summary>
    Task<bool> ExistsForEventAsync(
        Guid recipientId,
        string eventKey,
        CancellationToken cancellationToken = default);
}
