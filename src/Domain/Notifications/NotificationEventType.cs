namespace Domain.Notifications;

public enum NotificationEventType
{
    ///Objavljen sadržaj (obavještenje, materijal, zadatak).
    ContentPublished,

    ///Promejna roka.
    DeadlineChanged,

    ///Dostupan (objavljen) pregled rada.
    ReviewPublished
}
