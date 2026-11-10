using Domain.Notifications;

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

    /// <summary>
    /// Vraća sva obaveštenja jednog primaoca (ST-017, US-3).
    /// </summary>
    Task<IReadOnlyList<Notification>> ListForRecipientAsync(
        Guid recipientId,
        CancellationToken cancellationToken = default);
}
