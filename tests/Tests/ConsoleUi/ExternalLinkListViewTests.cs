using ConsoleUi.Materials;
using Domain.Materials;
using NUnit.Framework;

namespace Tests.ConsoleUi;

/// <summary>
/// ST-009, US-03: prikaz liste spoljnih veza materijala.
/// </summary>
[TestFixture]
public sealed class ExternalLinkListViewTests
{
    private static readonly Guid MaterialId = Guid.NewGuid();

    private static MaterialExternalLink Link(string url, Guid? materialId = null) =>
        new(Guid.NewGuid(), materialId ?? MaterialId, url);

    private static (int Count, string[] Lines) Show(IEnumerable<MaterialExternalLink> links, Guid? materialId = null)
    {
        var output = new StringWriter();
        var count = new ExternalLinkListView(output).Show(materialId ?? MaterialId, links);
        var lines = output.ToString()
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        return (count, lines);
    }

    [Test]
    public void Show_ListsAllLinksOfMaterialInGivenOrder()
    {
        var (count, lines) = Show(
        [
            Link("https://example.org/video-1"),
            Link("https://example.org/a-prezentacija"),
            Link("http://example.org/zadatak"),
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(3));
            Assert.That(lines, Is.EqualTo(new[]
            {
                "Spoljne veze materijala (3):",
                "  - https://example.org/video-1",
                "  - https://example.org/a-prezentacija",
                "  - http://example.org/zadatak",
            }));
        });
    }

    [Test]
    public void Show_SingleLink()
    {
        var (count, lines) = Show([Link("https://example.org/predavanje")]);

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(1));
            Assert.That(lines, Is.EqualTo(new[]
            {
                "Spoljne veze materijala (1):",
                "  - https://example.org/predavanje",
            }));
        });
    }

    [Test]
    public void Show_EmptyListSaysThereAreNoLinks()
    {
        var (count, lines) = Show([]);

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.Zero);
            Assert.That(lines, Is.EqualTo(new[]
            {
                "Spoljne veze materijala (0):",
                "  (nema spoljnih veza)",
            }));
        });
    }

    [Test]
    public void Show_DoesNotShowLinksOfAnotherMaterial()
    {
        var otherMaterialId = Guid.NewGuid();

        var (count, lines) = Show(
        [
            Link("https://example.org/moja-veza"),
            Link("https://example.org/tudja-veza", otherMaterialId),
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(1));
            Assert.That(lines, Has.Some.Contains("moja-veza"));
            Assert.That(lines, Has.None.Contains("tudja-veza"));
        });
    }

    [Test]
    public void Show_OnlyLinksOfAnotherMaterialMeansNoLinks()
    {
        var (count, lines) = Show([Link("https://example.org/tudja-veza", Guid.NewGuid())]);

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.Zero);
            Assert.That(lines, Has.Some.EqualTo("  (nema spoljnih veza)"));
        });
    }

    [Test]
    public void Show_ShowsSameAddressTwiceWhenGivenTwice()
    {
        var (count, _) = Show(
        [
            Link("https://example.org/isto"),
            Link("https://example.org/isto"),
        ]);

        Assert.That(count, Is.EqualTo(2));
    }

    [Test]
    public void Show_SkipsNullEntries()
    {
        var (count, _) = Show([Link("https://example.org/a"), null!]);

        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public void Show_RejectsEmptyMaterialWithoutWriting()
    {
        var output = new StringWriter();
        var view = new ExternalLinkListView(output);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => view.Show(Guid.Empty, []),
                Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("materialId"));
            Assert.That(output.ToString(), Is.Empty);
        });
    }

    [Test]
    public void Show_RejectsMissingLinkList()
    {
        var view = new ExternalLinkListView(new StringWriter());

        Assert.That(() => view.Show(MaterialId, null!), Throws.ArgumentNullException);
    }

    [Test]
    public void Constructor_RejectsMissingOutput()
    {
        Assert.That(() => new ExternalLinkListView(null!), Throws.ArgumentNullException);
    }
}
