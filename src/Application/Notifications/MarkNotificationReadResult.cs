namespace Application.Notifications;

/// <summary>
/// Ishod označavanja obaveštenja kao pročitanog (ST-017, US-4).
/// </summary>
public enum MarkNotificationReadResult
{
    /// <summary>Obaveštenje je pročitano (sada ili ranije).</summary>
    Marked,

    /// <summary>Obaveštenje ne postoji ili ne pripada korisniku; ništa nije promenjeno.</summary>
    NotFound
}
