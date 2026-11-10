using Domain.Notifications;

namespace Application.Notifications;

/// <summary>
/// Upis obaveštenja u skladište D1 (ST-017, US-2).
/// </summary>
public interface INotificationWriter
{
    /// <summary>
    /// Upisuje obaveštenje. Vraća false ako za istog primaoca već postoji
    /// obaveštenje sa istim ključem poslovnog događaja; tada se ništa ne upisuje.
    /// </summary>
    Task<bool> AddAsync(Notification notification, CancellationToken cancellationToken = default);
}
