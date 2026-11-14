using global::ConsoleUi;
using NUnit.Framework;

namespace Tests.ConsoleUi;

[TestFixture]
public sealed class ConsoleAppTests
{
    [Test]
    public async Task RunAsync_NoArguments_CompletesWithoutReadingInput()
    {
        var output = new StringWriter();

        // Čitač baca izuzetak na svaki pokušaj čitanja: test pada ako podrazumevani start zatraži unos.
        int exitCode = await ConsoleApp.RunAsync(Array.Empty<string>(), new ThrowingReader(), output);

        string text = output.ToString();
        Assert.That(exitCode, Is.EqualTo(ConsoleApp.ExitSuccess));
        Assert.That(text, Does.Contain("STUDIUM | Demonstracija čitanja kataloga (nije ST-003)"));
        Assert.That(text, Does.Not.Contain("ST-019"));
    }

    [Test]
    public async Task RunAsync_St019Option_AssignsSelectedGroups()
    {
        var output = new StringWriter();

        int exitCode = await ConsoleApp.RunAsync(new[] { ConsoleApp.St019Option }, new StringReader("1, 3"), output);

        string text = output.ToString();
        Assert.That(exitCode, Is.EqualTo(ConsoleApp.ExitSuccess));
        Assert.That(text, Does.Contain("ST-019"));
        Assert.That(text, Does.Contain("[USPEH] Dodeljeno 2 grupa nastavnom sadržaju."));
        Assert.That(text, Does.Not.Contain("Demonstracija čitanja kataloga"));
    }

    [Test]
    public async Task RunAsync_St019Option_DuplicateSelectionIsCountedOnce()
    {
        var output = new StringWriter();

        await ConsoleApp.RunAsync(new[] { ConsoleApp.St019Option }, new StringReader("2,2"), output);

        Assert.That(output.ToString(), Does.Contain("[USPEH] Dodeljeno 1 grupa nastavnom sadržaju."));
    }

    [TestCase("")]
    [TestCase("0")]
    [TestCase("4")]
    [TestCase("abc, -1, 99")]
    public async Task RunAsync_St019Option_NoValidSelection_AssignsNothing(string line)
    {
        var output = new StringWriter();

        int exitCode = await ConsoleApp.RunAsync(new[] { ConsoleApp.St019Option }, new StringReader(line), output);

        string text = output.ToString();
        Assert.That(exitCode, Is.EqualTo(ConsoleApp.ExitSuccess));
        Assert.That(text, Does.Not.Contain("[USPEH]"));
        Assert.That(text, Does.Contain("Nije izabrana nijedna važeća grupa"));
    }

    [Test]
    public async Task RunAsync_UnknownArgument_ReturnsErrorWithoutReadingInput()
    {
        var output = new StringWriter();

        int exitCode = await ConsoleApp.RunAsync(new[] { "--nepostojeci" }, new ThrowingReader(), output);

        Assert.That(exitCode, Is.EqualTo(ConsoleApp.ExitUnknownArgument));
        Assert.That(output.ToString(), Does.Contain(ConsoleApp.St019Option));
    }

    private sealed class ThrowingReader : TextReader
    {
        public override int Peek() => throw Unexpected();

        public override int Read() => throw Unexpected();

        public override int Read(char[] buffer, int index, int count) => throw Unexpected();

        public override string? ReadLine() => throw Unexpected();

        public override string ReadToEnd() => throw Unexpected();

        private static InvalidOperationException Unexpected() =>
            new("Podrazumevani CLI ne sme da čita interaktivni unos.");
    }
}
