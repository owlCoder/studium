using ConsoleUi;
using NUnit.Framework;

namespace Tests.Academic;

[TestFixture]
public sealed class St004DemoSmokeTests
{
    [Test]
    public async Task Demo_RunsAllSteps_AndRejectsInvalidOnes()
    {
        var original = Console.Out;
        var writer = new StringWriter();
        Console.SetOut(writer);
        try
        {
            await St004Demo.RunAsync();
        }
        finally
        {
            Console.SetOut(original);
        }

        var output = writer.ToString();
        Assert.That(output, Does.Contain("Godina otvorena (id"));
        Assert.That(output, Does.Contain("Izvođenje otvoreno (id"));
        Assert.That(output.Split("ODBIJENO").Length - 1, Is.EqualTo(3));
    }
}
