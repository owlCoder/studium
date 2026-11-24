using NUnit.Framework;
using Domain.Notifications;

namespace Tests.Domain;

[TestFixture]
public sealed class NotificationTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 11, 10, 12, 0, 0, TimeSpan.FromHours(1));

    private static SourceReference AnySource() =>
        new(SourceType.Assignment, Guid.NewGuid());

    private static Notification CreateValid(
        string summary = "Objavljen pregled rada: Zadatak 1",
        string eventKey = "review-published:42") =>
        Notification.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            NotificationEventType.ReviewPublished,
            AnySource(),
            summary,
            eventKey,
            CreatedAt);

    [Test]
    public void Create_ValidData_IsUnread()
    {
        var notification = CreateValid();

        Assert.That(notification.IsRead, Is.False);
    }

    [Test]
    public void Create_TrimsSummaryAndEventKey()
    {
        var notification = CreateValid(summary: "  Promenjen rok  ", eventKey: " deadline-changed:7 ");

        Assert.Multiple(() =>
        {
            Assert.That(notification.Summary, Is.EqualTo("Promenjen rok"));
            Assert.That(notification.EventKey, Is.EqualTo("deadline-changed:7"));
            Assert.That(notification.CreatedAt, Is.EqualTo(CreatedAt));
        });
    }

    [TestCase(NotificationEventType.ContentPublished)]
    [TestCase(NotificationEventType.DeadlineChanged)]
    [TestCase(NotificationEventType.ReviewPublished)]
    public void Create_AcceptsEachDefinedEventType(NotificationEventType eventType)
    {
        var notification = Notification.Create(
            Guid.NewGuid(), Guid.NewGuid(), eventType, AnySource(), "Opis", "kljuc:1", CreatedAt);

        Assert.That(notification.EventType, Is.EqualTo(eventType));
    }

    [Test]
    public void Create_RejectsUndefinedEventType()
    {
        Assert.That(
            () => Notification.Create(
                Guid.NewGuid(), Guid.NewGuid(), (NotificationEventType)99, AnySource(), "Opis", "kljuc:1", CreatedAt),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Create_RejectsBlankSummary(string summary)
    {
        Assert.That(() => CreateValid(summary: summary), Throws.TypeOf<ArgumentException>());
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Create_RejectsBlankEventKey(string eventKey)
    {
        Assert.That(() => CreateValid(eventKey: eventKey), Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Create_RejectsEmptyRecipient()
    {
        Assert.That(
            () => Notification.Create(
                Guid.NewGuid(), Guid.Empty, NotificationEventType.ReviewPublished, AnySource(), "Opis", "kljuc:1", CreatedAt),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Create_RejectsMissingSource()
    {
        Assert.That(
            () => Notification.Create(
                Guid.NewGuid(), Guid.NewGuid(), NotificationEventType.ReviewPublished, null!, "Opis", "kljuc:1", CreatedAt),
            Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void SourceReference_EqualByValue()
    {
        var id = Guid.NewGuid();

        Assert.That(new SourceReference(SourceType.Material, id), Is.EqualTo(new SourceReference(SourceType.Material, id)));
    }

    [Test]
    public void SourceReference_RejectsEmptyId()
    {
        Assert.That(() => new SourceReference(SourceType.Material, Guid.Empty), Throws.TypeOf<ArgumentException>());
    }
}
