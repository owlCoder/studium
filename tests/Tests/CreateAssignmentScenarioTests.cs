using Application.Assignments;
using ConsoleUi.Scenarios;
using Infrastructure.Demo;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Tests;

[TestFixture]
public sealed class CreateAssignmentScenarioTests
{
    [Test]
    public async Task Confirm_SavesEditedValuesThroughApi()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        using var input = new StringReader($"""
            {DemoData.ErsId}
            1
            Prvi naziv
            2
            Opis zadatka
            3
            Uputstvo za rad
            4
            15.01.2027 18:30 +01:00
            4
            31.02.2027 18:30 +01:00
            1
            Ispravljen naziv
            5
            """);
        using var output = new StringWriter();

        await CreateAssignmentScenario.RunAsync(client, input, output);

        var lines = output.ToString().TrimEnd().Split(Environment.NewLine);
        var lastLine = lines[^1];
        Assert.That(output.ToString(), Does.Contain("Rok nije ispravan."));
        Assert.That(lastLine, Does.Contain("Nacrt je sačuvan. ID zadatka: "));
        var id = Guid.Parse(lastLine.Split(' ').Last());
        var repository = factory.Services.GetRequiredService<IAssignmentRepository>();
        var draft = await repository.FindAsync(id);
        Assert.That(draft, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(draft!.CourseOfferingId, Is.EqualTo(DemoData.ErsId));
            Assert.That(draft.Title, Is.EqualTo("Ispravljen naziv"));
            Assert.That(draft.Description, Is.EqualTo("Opis zadatka"));
            Assert.That(draft.Instructions, Is.EqualTo("Uputstvo za rad"));
            var expectedDeadline = new DateTimeOffset(2027, 1, 15, 18, 30, 0, TimeSpan.FromHours(1));
            Assert.That(draft.Deadline?.EqualsExact(expectedDeadline), Is.True);
        });
    }

    [Test]
    public async Task Confirm_ShowsForbiddenForAnotherOffering()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        using var input = new StringReader($"{DemoData.OibId}\n1\nDomaći zadatak\n5");
        using var output = new StringWriter();

        await CreateAssignmentScenario.RunAsync(client, input, output);

        Assert.That(output.ToString(), Does.Contain("Nemaš pravo da kreiraš zadatak za ovo izvođenje."));
    }

    [TestCase("5\n0", "Naziv zadatka je obavezan.")]
    [TestCase("1\nDomaći zadatak\n0", "Unos je otkazan.")]
    public async Task Form_DoesNotSubmitWithoutValidConfirmation(string choices, string message)
    {
        using var client = new HttpClient();
        using var input = new StringReader($"{DemoData.ErsId}\n{choices}");
        using var output = new StringWriter();

        await CreateAssignmentScenario.RunAsync(client, input, output);

        Assert.That(output.ToString(), Does.Contain(message));
    }
}
