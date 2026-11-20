using NUnit.Framework;
using Domain.Notifications;
using Infrastructure.Notifications;

namespace Tests.Infrastructure;

[TestFixture]
public sealed class InMemoryNotificationRepositoryTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 11, 10, 12, 0, 0, TimeSpan.FromHours(1));

    private static Notification CreateNotification(Guid recipientId, string eventKey) =>
        Notification.Create(
            Guid.NewGuid(),
            recipientId,
            NotificationEventType.ReviewPublished,
            new SourceReference(SourceType.Submission, Guid.NewGuid()),
            "Objavljen pregled rada",
            eventKey,
            CreatedAt);

    [Test]
    public async Task ExistsForEventAsync_ReturnsTrueAfterFirstAdd()
    {
        var repository = new InMemoryNotificationRepository();
        var recipientId = Guid.NewGuid();

        var added = await repository.AddAsync(CreateNotification(recipientId, "review-published:42"));
        var exists = await repository.ExistsForEventAsync(recipientId, "review-published:42");

        Assert.Multiple(() =>
        {
            Assert.That(added, Is.True);
            Assert.That(exists, Is.True);
        });
    }

    [Test]
    public async Task AddAsync_SameRecipientAndEventTwice_AddsOnlyOnce()
    {
        var repository = new InMemoryNotificationRepository();
        var recipientId = Guid.NewGuid();

        var first = await repository.AddAsync(CreateNotification(recipientId, "review-published:42"));
        var second = await repository.AddAsync(CreateNotification(recipientId, "review-published:42"));

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.True);
            Assert.That(second, Is.False);
        });
    }

    [Test]
    public async Task AddAsync_SameEventForTwoRecipients_AddsBoth()
    {
        var repository = new InMemoryNotificationRepository();
        var firstRecipient = Guid.NewGuid();
        var secondRecipient = Guid.NewGuid();

        var first = await repository.AddAsync(CreateNotification(firstRecipient, "content-published:7"));
        var second = await repository.AddAsync(CreateNotification(secondRecipient, "content-published:7"));

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.True);
            Assert.That(second, Is.True);
        });
    }

    [Test]
    public async Task ExistsForEventAsync_ReturnsFalseForUnknownEvent()
    {
        var repository = new InMemoryNotificationRepository();

        var exists = await repository.ExistsForEventAsync(Guid.NewGuid(), "deadline-changed:1");

        Assert.That(exists, Is.False);
    }

    [Test]
    public void AddAsync_RejectsMissingNotification()
    {
        var repository = new InMemoryNotificationRepository();

        Assert.That(() => repository.AddAsync(null!), Throws.TypeOf<ArgumentNullException>());
    }
}
