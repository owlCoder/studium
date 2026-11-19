using ConsoleUi.Materials;
using Domain.Materials;
using NUnit.Framework;

namespace Tests.ConsoleUi;

/// <summary>
/// ST-009, US-03: konzolno polje za unos spoljne veze materijala.
/// </summary>
[TestFixture]
public sealed class ExternalLinkFieldTests
{
    private static readonly Guid MaterialId = Guid.NewGuid();

    private static (MaterialExternalLink? Link, string Output) Run(string userInput, Guid? materialId = null)
    {
        var output = new StringWriter();
        var field = new ExternalLinkField(new StringReader(userInput), output);

        var link = field.Read(materialId ?? MaterialId);
        return (link, output.ToString());
    }

    [Test]
    public void Read_ReturnsLinkBoundToMaterial()
    {
        var (link, output) = Run("https://example.org/predavanje-1\n");

        Assert.That(link, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(link!.MaterialId, Is.EqualTo(MaterialId));
            Assert.That(link.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(link.Url.AbsoluteUri, Is.EqualTo("https://example.org/predavanje-1"));
            Assert.That(output, Does.Contain(ExternalLinkField.Prompt));
            Assert.That(output, Does.Contain("Uneta veza: https://example.org/predavanje-1"));
        });
    }

    [Test]
    public void Read_TrimsSurroundingWhitespace()
    {
        var (link, _) = Run("   https://example.org/a   \n");

        Assert.That(link!.Url.AbsoluteUri, Is.EqualTo("https://example.org/a"));
    }

    [TestCase("\n")]
    [TestCase("   \n")]
    public void Read_EmptyInputMeansNoLink(string userInput)
    {
        var (link, output) = Run(userInput);

        Assert.Multiple(() =>
        {
            Assert.That(link, Is.Null);
            Assert.That(output, Does.Contain("Veza nije dodata."));
        });
    }

    [Test]
    public void Read_EndOfInputCancelsWithoutLink()
    {
        var (link, output) = Run(string.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(link, Is.Null);
            Assert.That(output, Does.Contain("Unos je prekinut. Veza nije dodata."));
        });
    }

    [TestCase("www.example.org")]
    [TestCase("https://")]
    [TestCase("javascript:alert(1)")]
    public void Read_InvalidAddressExplainsReasonAndAsksAgain(string invalid)
    {
        var (link, output) = Run($"{invalid}\nhttps://example.org/video\n");

        Assert.Multiple(() =>
        {
            Assert.That(link!.Url.AbsoluteUri, Is.EqualTo("https://example.org/video"));
            Assert.That(output, Does.Contain("Veza nije prihvaćena:"));
            Assert.That(CountOccurrences(output, ExternalLinkField.Prompt), Is.EqualTo(2));
        });
    }

    [Test]
    public void Read_ErrorMessageDoesNotShowTechnicalParameterName()
    {
        var (_, output) = Run("www.example.org\n\n");

        Assert.Multiple(() =>
        {
            Assert.That(output, Does.Contain("https://"));
            Assert.That(output, Does.Not.Contain("(Parameter"));
        });
    }

    [Test]
    public void Read_InvalidAddressThenEmptyInputMeansNoLink()
    {
        var (link, output) = Run("www.example.org\n\n");

        Assert.Multiple(() =>
        {
            Assert.That(link, Is.Null);
            Assert.That(output, Does.Contain("Veza nije dodata."));
        });
    }

    [Test]
    public void Read_InvalidAddressThenEndOfInputCancels()
    {
        var (link, output) = Run("www.example.org\n");

        Assert.Multiple(() =>
        {
            Assert.That(link, Is.Null);
            Assert.That(output, Does.Contain("Unos je prekinut."));
        });
    }

    [Test]
    public void Read_AcceptsHttpAddress()
    {
        var (link, _) = Run("http://example.org\n");

        Assert.That(link!.Url.Scheme, Is.EqualTo("http"));
    }

    [Test]
    public void Read_RejectsEmptyMaterialBeforeAsking()
    {
        var output = new StringWriter();
        var field = new ExternalLinkField(new StringReader("https://example.org\n"), output);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => field.Read(Guid.Empty),
                Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("materialId"));
            Assert.That(output.ToString(), Is.Empty);
        });
    }

    [Test]
    public void Constructor_RejectsMissingInputOrOutput()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => new ExternalLinkField(null!, new StringWriter()), Throws.ArgumentNullException);
            Assert.That(() => new ExternalLinkField(new StringReader(""), null!), Throws.ArgumentNullException);
        });
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }
        return count;
    }
}
