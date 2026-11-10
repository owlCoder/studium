using NUnit.Framework;
using Domain.Materials;

namespace Tests.Domain;

[TestFixture]
public sealed class MaterialExternalLinkTests
{
    [Test]
    public void Constructor_CreatesLinkBoundToMaterial()
    {
        var id = Guid.NewGuid();
        var materialId = Guid.NewGuid();

        var link = new MaterialExternalLink(id, materialId, "https://example.org/predavanje-1");

        Assert.Multiple(() =>
        {
            Assert.That(link.Id, Is.EqualTo(id));
            Assert.That(link.MaterialId, Is.EqualTo(materialId));
            Assert.That(link.Url, Is.EqualTo(new Uri("https://example.org/predavanje-1")));
        });
    }

    [Test]
    public void Constructor_TrimsSurroundingWhitespaceFromUrl()
    {
        var link = new MaterialExternalLink(Guid.NewGuid(), Guid.NewGuid(), "  https://example.org/a  ");

        Assert.That(link.Url.AbsoluteUri, Is.EqualTo("https://example.org/a"));
    }

    [Test]
    public void Constructor_PreservesQueryAndFragment()
    {
        var link = new MaterialExternalLink(
            Guid.NewGuid(), Guid.NewGuid(), "https://example.org/video?t=120#deo-2");

        Assert.Multiple(() =>
        {
            Assert.That(link.Url.Query, Is.EqualTo("?t=120"));
            Assert.That(link.Url.Fragment, Is.EqualTo("#deo-2"));
        });
    }

    [Test]
    public void Constructor_AcceptsMinimalAbsoluteUrl()
    {
        var link = new MaterialExternalLink(Guid.NewGuid(), Guid.NewGuid(), "http://a");

        Assert.That(link.Url.Host, Is.EqualTo("a"));
    }

    [Test]
    public void Constructor_RejectsEmptyId()
    {
        Assert.That(
            () => new MaterialExternalLink(Guid.Empty, Guid.NewGuid(), "https://example.org"),
            Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("id"));
    }

    [Test]
    public void Constructor_RejectsEmptyMaterial()
    {
        Assert.That(
            () => new MaterialExternalLink(Guid.NewGuid(), Guid.Empty, "https://example.org"),
            Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("materialId"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_RejectsBlankUrl(string? url)
    {
        Assert.That(
            () => new MaterialExternalLink(Guid.NewGuid(), Guid.NewGuid(), url!),
            Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("url"));
    }

    [TestCase("example.org/predavanje")]
    [TestCase("materijali/predavanje-1")]
    [TestCase("https://")]
    [TestCase("nije adresa")]
    public void Constructor_RejectsMalformedOrRelativeUrl(string url)
    {
        Assert.That(
            () => new MaterialExternalLink(Guid.NewGuid(), Guid.NewGuid(), url),
            Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("url"));
    }
}
