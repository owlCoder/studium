using ConsoleUi.Materials;
using NUnit.Framework;

namespace Tests.ConsoleUi;

/// <summary>
/// ST-009, US-11 (stavka 2): čitljiv prikaz veličine priloga.
/// </summary>
[TestFixture]
public sealed class FileSizeFormatterTests
{
    [TestCase(0L, "0 B")]
    [TestCase(1L, "1 B")]
    [TestCase(1023L, "1023 B")]
    [TestCase(1024L, "1 KB")]
    [TestCase(1536L, "1.5 KB")]
    [TestCase(1024L * 1024 - 1, "1024 KB")]
    [TestCase(1024L * 1024, "1 MB")]
    [TestCase(1024L * 1024 * 3 / 2, "1.5 MB")]
    [TestCase(20L * 1024 * 1024, "20 MB")]
    public void Format_ReturnsReadableSize(long sizeInBytes, string expected)
    {
        Assert.That(FileSizeFormatter.Format(sizeInBytes), Is.EqualTo(expected));
    }

    [Test]
    public void Format_NegativeSize_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FileSizeFormatter.Format(-1));
    }
}
