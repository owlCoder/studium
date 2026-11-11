using NUnit.Framework;
using Application.Access;
using Application.Search;
using Domain.Search;

namespace Tests.Application;

[TestFixture]
public sealed class SearchResultDtoTests
{
    private static SearchableItem CreateItem(ContentType type, string description) =>
        new(Guid.NewGuid(), type, Guid.NewGuid(), "Priprema za kolokvijum", description, ContentState.Published);

    [Test]
    public void FromItem_CopiesTitleTypeAndSnippet()
    {
        var item = CreateItem(ContentType.Material, "Zadaci za kolokvijum iz prošle godine");

        var dto = SearchResultDto.FromItem(item, "kolokvijum");

        Assert.Multiple(() =>
        {
            Assert.That(dto.Id, Is.EqualTo(item.Id));
            Assert.That(dto.Title, Is.EqualTo("Priprema za kolokvijum"));
            Assert.That(dto.Type, Is.EqualTo(ContentType.Material));
            Assert.That(dto.Snippet, Is.EqualTo("Zadaci za kolokvijum iz prošle godine"));
        });
    }

    [TestCase(ContentType.Material, "/materials/")]
    [TestCase(ContentType.Assignment, "/assignments/")]
    [TestCase(ContentType.Announcement, "/announcements/")]
    [TestCase(ContentType.CourseOffering, "/offerings/")]
    public void FromItem_BuildsLinkToSource(ContentType type, string expectedPrefix)
    {
        var item = CreateItem(type, "Opis");

        var dto = SearchResultDto.FromItem(item, "kolokvijum");

        Assert.That(dto.Link, Is.EqualTo(expectedPrefix + item.Id));
    }

    [Test]
    public void FromItem_RejectsMissingItem()
    {
        Assert.That(() => SearchResultDto.FromItem(null!, "kolokvijum"), Throws.TypeOf<ArgumentNullException>());
    }
}
