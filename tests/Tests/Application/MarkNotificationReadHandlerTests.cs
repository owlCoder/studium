using Moq;
using NUnit.Framework;
using Application.Notifications;
using Domain.Notifications;

namespace Tests.Application;

[TestFixture]
public sealed class MarkNotificationReadHandlerTests
{
    private static Notification CreateNotification(Guid recipientId) =>
        Notification.Create(
            Guid.NewGuid(),
            recipientId,
            NotificationEventType.ReviewPublished,
            new SourceReference(SourceType.Submission, Guid.NewGuid()),
            "Objavljen pregled rada",
            $"review-published:{Guid.NewGuid()}",
            new DateTimeOffset(2026, 11, 10, 12, 0, 0, TimeSpan.Zero));

    [Test]
    public async Task HandleAsync_OwnNotification_MarksAsReadAndSaves()
    {
        var me = Guid.NewGuid();
        var notification = CreateNotification(me);
        var reader = new Mock<INotificationReader>();
        reader.Setup(x => x.GetByIdAsync(notification.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);
        var writer = new Mock<INotificationWriter>();

        var result = await new MarkNotificationReadHandler(reader.Object, writer.Object)
            .HandleAsync(notification.Id, me);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(MarkNotificationReadResult.Marked));
            Assert.That(notification.IsRead, Is.True);
        });
        writer.Verify(x => x.SaveAsync(notification, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_AlreadyRead_ChangesNothing()
    {
        var me = Guid.NewGuid();
        var notification = CreateNotification(me);
        notification.MarkAsRead();
        var reader = new Mock<INotificationReader>();
        reader.Setup(x => x.GetByIdAsync(notification.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);
        var writer = new Mock<INotificationWriter>();

        var result = await new MarkNotificationReadHandler(reader.Object, writer.Object)
            .HandleAsync(notification.Id, me);

        Assert.That(result, Is.EqualTo(MarkNotificationReadResult.Marked));
        writer.Verify(x => x.SaveAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_SomeoneElsesNotification_LooksLikeNotFoundAndChangesNothing()
    {
        var owner = Guid.NewGuid();
        var notification = CreateNotification(owner);
        var reader = new Mock<INotificationReader>();
        reader.Setup(x => x.GetByIdAsync(notification.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);
        var writer = new Mock<INotificationWriter>();

        var result = await new MarkNotificationReadHandler(reader.Object, writer.Object)
            .HandleAsync(notification.Id, Guid.NewGuid());

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(MarkNotificationReadResult.NotFound));
            Assert.That(notification.IsRead, Is.False);
        });
        writer.Verify(x => x.SaveAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_UnknownNotification_ReturnsNotFound()
    {
        var reader = new Mock<INotificationReader>();
        reader.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Notification?)null);
        var writer = new Mock<INotificationWriter>();

        var result = await new MarkNotificationReadHandler(reader.Object, writer.Object)
            .HandleAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.That(result, Is.EqualTo(MarkNotificationReadResult.NotFound));
    }
}
