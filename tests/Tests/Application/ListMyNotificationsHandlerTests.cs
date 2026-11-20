using Moq;
using NUnit.Framework;
using Application.Notifications;
using Domain.Notifications;

namespace Tests.Application;

[TestFixture]
public sealed class ListMyNotificationsHandlerTests
{
    private static Notification CreateNotification(Guid recipientId, string summary, DateTimeOffset createdAt) =>
        Notification.Create(
            Guid.NewGuid(),
            recipientId,
            NotificationEventType.ContentPublished,
            new SourceReference(SourceType.Material, Guid.NewGuid()),
            summary,
            $"content-published:{Guid.NewGuid()}",
            createdAt);

    [Test]
    public async Task HandleAsync_ReturnsOnlyRecipientsNotifications()
    {
        var me = Guid.NewGuid();
        var someoneElse = Guid.NewGuid();
        IReadOnlyList<Notification> stored = new[]
        {
            CreateNotification(me, "Moje", new DateTimeOffset(2026, 11, 10, 9, 0, 0, TimeSpan.Zero)),
            CreateNotification(someoneElse, "Tudje", new DateTimeOffset(2026, 11, 10, 10, 0, 0, TimeSpan.Zero))
        };
        var reader = new Mock<INotificationReader>();
        reader.Setup(x => x.ListForRecipientAsync(me, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);

        var result = await new ListMyNotificationsHandler(reader.Object).HandleAsync(me);

        Assert.That(result.Select(x => x.Summary), Is.EqualTo(new[] { "Moje" }));
        reader.Verify(x => x.ListForRecipientAsync(me, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_SortsNewestFirst()
    {
        var me = Guid.NewGuid();
        IReadOnlyList<Notification> stored = new[]
        {
            CreateNotification(me, "Starije", new DateTimeOffset(2026, 11, 8, 9, 0, 0, TimeSpan.Zero)),
            CreateNotification(me, "Najnovije", new DateTimeOffset(2026, 11, 10, 9, 0, 0, TimeSpan.Zero)),
            CreateNotification(me, "Srednje", new DateTimeOffset(2026, 11, 9, 9, 0, 0, TimeSpan.Zero))
        };
        var reader = new Mock<INotificationReader>();
        reader.Setup(x => x.ListForRecipientAsync(me, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);

        var result = await new ListMyNotificationsHandler(reader.Object).HandleAsync(me);

        Assert.That(result.Select(x => x.Summary), Is.EqualTo(new[] { "Najnovije", "Srednje", "Starije" }));
    }

    [Test]
    public async Task HandleAsync_ReturnsEmptyListWhenThereAreNoNotifications()
    {
        var me = Guid.NewGuid();
        var reader = new Mock<INotificationReader>();
        reader.Setup(x => x.ListForRecipientAsync(me, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Notification>());

        var result = await new ListMyNotificationsHandler(reader.Object).HandleAsync(me);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void HandleAsync_RejectsEmptyRecipient()
    {
        var reader = new Mock<INotificationReader>();

        Assert.That(
            () => new ListMyNotificationsHandler(reader.Object).HandleAsync(Guid.Empty),
            Throws.TypeOf<ArgumentException>());
    }
}
