using NUnit.Framework;
using Application.Notifications;
using Domain.Notifications;

namespace Tests.Application;

[TestFixture]
public sealed class NotificationListItemDtoTests
{
    private static Notification CreateNotification(DateTimeOffset createdAt) =>
        Notification.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            NotificationEventType.DeadlineChanged,
            new SourceReference(SourceType.Assignment, Guid.NewGuid()),
            "Promenjen rok za Zadatak 2",
            "deadline-changed:15",
            createdAt);

    [Test]
    public void FromNotification_CopiesVisibleData()
    {
        var notification = CreateNotification(new DateTimeOffset(2026, 11, 10, 9, 30, 0, TimeSpan.Zero));

        var dto = NotificationListItemDto.FromNotification(notification);

        Assert.Multiple(() =>
        {
            Assert.That(dto.Id, Is.EqualTo(notification.Id));
            Assert.That(dto.EventType, Is.EqualTo(NotificationEventType.DeadlineChanged));
            Assert.That(dto.Summary, Is.EqualTo("Promenjen rok za Zadatak 2"));
            Assert.That(dto.IsRead, Is.False);
        });
    }

    [Test]
    public void FromNotification_FormatsCreatedAt()
    {
        var notification = CreateNotification(new DateTimeOffset(2026, 3, 5, 8, 7, 0, TimeSpan.Zero));

        var dto = NotificationListItemDto.FromNotification(notification);

        Assert.That(dto.CreatedAt, Is.EqualTo("05.03.2026. 08:07"));
    }

    [Test]
    public void FromNotification_ShowsReadState()
    {
        var notification = CreateNotification(new DateTimeOffset(2026, 11, 10, 9, 30, 0, TimeSpan.Zero));
        notification.MarkAsRead();

        var dto = NotificationListItemDto.FromNotification(notification);

        Assert.That(dto.IsRead, Is.True);
    }

    [Test]
    public void FromNotification_RejectsMissingNotification()
    {
        Assert.That(
            () => NotificationListItemDto.FromNotification(null!),
            Throws.TypeOf<ArgumentNullException>());
    }
}
