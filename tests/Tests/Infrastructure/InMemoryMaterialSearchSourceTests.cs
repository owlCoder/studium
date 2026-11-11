using NUnit.Framework;
using Application.Access;
using Application.Search;
using Domain.Search;
using Infrastructure.Search;

namespace Tests.Infrastructure;

[TestFixture]
public sealed class InMemoryMaterialSearchSourceTests
{
    private static readonly Guid Ers = Guid.NewGuid();
    private static readonly Guid Oib = Guid.NewGuid();

    private static InMemoryMaterialSearchSource CreateSource() =>
        new(new[]
        {
            new SearchableItem(Guid.NewGuid(), ContentType.Material, Ers,
                "Priprema za kolokvijum", "Zadaci sa prošlogodišnjeg kolokvijuma", ContentState.Published),
            new SearchableItem(Guid.NewGuid(), ContentType.Material, Ers,
                "Rešenja (nacrt)", "Rešenja za Kolokvijum 1, još se ne objavljuju", ContentState.Draft),
            new SearchableItem(Guid.NewGuid(), ContentType.Material, Oib,
                "Uvod u bezbednost", "Prvo predavanje", ContentState.Published),
            new SearchableItem(Guid.NewGuid(), ContentType.Assignment, Ers,
                "Domaći zadatak 1", "Kolokvijum ne ulazi ovde", ContentState.Published)
        });

    [Test]
    public async Task SearchAsync_FindsTextInTitleOrDescription_IgnoringCase()
    {
        var result = await CreateSource().SearchAsync("KOLOKVIJUM", null);

        Assert.That(result.Select(x => x.Title),
            Is.EquivalentTo(new[] { "Priprema za kolokvijum", "Rešenja (nacrt)" }));
    }

    [Test]
    public async Task SearchAsync_ReturnsOnlyMaterials()
    {
        var result = await CreateSource().SearchAsync("kolokvijum", null);

        Assert.That(result.All(x => x.Type == ContentType.Material), Is.True);
    }

    [Test]
    public async Task SearchAsync_LimitsToSelectedOffering()
    {
        var result = await CreateSource().SearchAsync("e", Oib);

        Assert.That(result.Select(x => x.Title), Is.EqualTo(new[] { "Uvod u bezbednost" }));
    }

    [Test]
    public async Task SearchAsync_ReturnsEmptyListWhenNothingMatches()
    {
        var result = await CreateSource().SearchAsync("ispit iz fizike", null);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Source_ReportsMaterialType()
    {
        Assert.That(CreateSource().Type, Is.EqualTo(ContentType.Material));
    }
}
