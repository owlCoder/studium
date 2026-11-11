using NUnit.Framework;
using Domain.Search;

namespace Tests.Domain;

[TestFixture]
public sealed class SearchQueryTests
{
    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_RejectsBlankText(string text)
    {
        Assert.That(() => new SearchQuery(text), Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Constructor_TrimsText()
    {
        var query = new SearchQuery("  kolokvijum  ");

        Assert.That(query.Text, Is.EqualTo("kolokvijum"));
    }

    [Test]
    public void Constructor_WithoutTypes_SearchesAllTypes()
    {
        var query = new SearchQuery("kolokvijum");

        Assert.That(query.Types, Is.EquivalentTo(Enum.GetValues<ContentType>()));
    }

    [Test]
    public void Constructor_WithTypes_SearchesOnlySelectedTypes()
    {
        var query = new SearchQuery("kolokvijum", types: new[] { ContentType.Material });

        Assert.Multiple(() =>
        {
            Assert.That(query.Includes(ContentType.Material), Is.True);
            Assert.That(query.Includes(ContentType.Assignment), Is.False);
        });
    }

    [Test]
    public void Constructor_RejectsEmptyOffering()
    {
        Assert.That(() => new SearchQuery("kolokvijum", Guid.Empty), Throws.TypeOf<ArgumentException>());
    }
}
