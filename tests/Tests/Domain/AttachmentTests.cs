using NUnit.Framework;
using Domain.Submissions;

namespace Tests.Domain;

[TestFixture]
public class AttachmentTests
{
    [Test]
    public void Constructor_SetsValues()
    {
        var attachment = new Attachment(" rad.pdf ", "application/pdf", 2048);

        Assert.That(attachment.FileName, Is.EqualTo("rad.pdf"));
        Assert.That(attachment.FileType, Is.EqualTo("application/pdf"));
        Assert.That(attachment.Size, Is.EqualTo(2048));
    }

    [Test]
    public void Constructor_RejectsBlankFileName()
    {
        Assert.That(
            () => new Attachment(" ", "application/pdf", 2048),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Constructor_RejectsBlankFileType()
    {
        Assert.That(
            () => new Attachment("rad.pdf", "", 2048),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Constructor_RejectsZeroSize()
    {
        Assert.That(
            () => new Attachment("rad.pdf", "application/pdf", 0),
            Throws.TypeOf<ArgumentException>());
    }
}