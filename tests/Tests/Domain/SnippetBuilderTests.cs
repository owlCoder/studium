using NUnit.Framework;
using Domain.Search;

namespace Tests.Domain;

[TestFixture]
public sealed class SnippetBuilderTests
{
    private static readonly string LongDescription =
        "Ovaj materijal sadrzi pregled svih tema koje su obradjene na predavanjima i vezbama tokom prvog dela semestra, " +
        "ukljucujuci primere zadataka, objasnjenja i preporucenu literaturu. Posebno je naglasena priprema za kolokvijum " +
        "koji se odrzava krajem novembra, kao i nacin bodovanja i pravila ponasanja tokom samog ispita.";

    [Test]
    public void Build_ShortDescription_ReturnsWholeText()
    {
        Assert.That(SnippetBuilder.Build("  Kratak opis za kolokvijum ", "kolokvijum"), Is.EqualTo("Kratak opis za kolokvijum"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Build_EmptyDescription_ReturnsEmptySnippet(string? description)
    {
        Assert.That(SnippetBuilder.Build(description, "kolokvijum"), Is.Empty);
    }

    [Test]
    public void Build_LongDescription_IsAtMost160Characters()
    {
        var snippet = SnippetBuilder.Build(LongDescription, "kolokvijum");

        Assert.That(snippet.Length, Is.LessThanOrEqualTo(SnippetBuilder.MaxLength));
    }

    [Test]
    public void Build_LongDescription_ContainsTheMatch()
    {
        var snippet = SnippetBuilder.Build(LongDescription, "KOLOKVIJUM");

        Assert.That(snippet, Does.Contain("kolokvijum"));
    }

    [Test]
    public void Build_LongDescription_DoesNotCutWords()
    {
        var snippet = SnippetBuilder.Build(LongDescription, "kolokvijum");
        var words = LongDescription.Split(' ');

        Assert.That(snippet.Split(' '), Is.SubsetOf(words));
    }

    [Test]
    public void Build_NoMatch_StartsFromBeginning()
    {
        var snippet = SnippetBuilder.Build(LongDescription, "fizika");

        Assert.That(LongDescription, Does.StartWith(snippet));
    }
}
