using Application.Notifications;
using Domain.Notifications;

namespace Infrastructure.Notifications;

/// <summary>
/// Memorijsko skladište obaveštenja (ST-017, US-2); nema trajnog čuvanja podataka.
/// Isti par primalac + ključ događaja upisuje se najviše jednom.
/// </summary>
public sealed class InMemoryNotificationRepository : INotificationWriter, INotificationReader
{
    private readonly List<Notification> _notifications = new();
    private readonly object _sync = new();

    public Task<bool> AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            if (ContainsEvent(notification.RecipientId, notification.EventKey))
                return Task.FromResult(false);

            _notifications.Add(notification);
            return Task.FromResult(true);
        }
    }

    public Task<bool> ExistsForEventAsync(
        Guid recipientId,
        string eventKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            return Task.FromResult(ContainsEvent(recipientId, eventKey));
        }
    }

    private bool ContainsEvent(Guid recipientId, string eventKey) =>
        _notifications.Any(n =>
            n.RecipientId == recipientId &&
            string.Equals(n.EventKey, eventKey?.Trim(), StringComparison.Ordinal));
}
