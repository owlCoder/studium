using Application.Materials;
using ConsoleUi.Materials;
using Domain.Materials;
using Infrastructure.Materials;
using NUnit.Framework;

namespace Tests.ConsoleUi;

/// <summary>
/// ST-009: konzolna forma za unos naziva i opisa novog materijala.
/// </summary>
[TestFixture]
public sealed class CreateMaterialFormTests
{
    private static readonly Guid TeacherId = Guid.NewGuid();
    private static readonly Guid OfferingId = Guid.NewGuid();

    private InMemoryMaterialRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new InMemoryMaterialRepository();
    }

    private async Task<(Guid? Id, string Output)> RunFormAsync(string userInput, Guid? userId = null)
    {
        var output = new StringWriter();
        var form = new CreateMaterialForm(
            new CreateMaterialHandler(
                _repository,
                new InMemoryOfferingStaffChecker([(TeacherId, OfferingId)])),
            new StringReader(userInput),
            output);

        var id = await form.RunAsync(userId ?? TeacherId, OfferingId);
        return (id, output.ToString());
    }

    [Test]
    public async Task RunAsync_SavesNameAndDescriptionAsDraft()
    {
        var (id, output) = await RunFormAsync(" Predavanje 1 \n Uvod u predmet \n");

        Assert.That(id, Is.Not.Null);
        var saved = await _repository.GetByIdAsync(id!.Value);
        Assert.Multiple(() =>
        {
            Assert.That(saved!.Name, Is.EqualTo("Predavanje 1"));
            Assert.That(saved.Description, Is.EqualTo("Uvod u predmet"));
            Assert.That(saved.CourseOfferingId, Is.EqualTo(OfferingId));
            Assert.That(saved.PublicationState, Is.EqualTo(MaterialPublicationState.Draft));
            Assert.That(output, Does.Contain("Materijal je sačuvan kao nacrt."));
            Assert.That(output, Does.Contain("Naziv: Predavanje 1"));
        });
    }

    [Test]
    public async Task RunAsync_AllowsEmptyDescription()
    {
        var (id, output) = await RunFormAsync("Predavanje 1\n\n");

        var saved = await _repository.GetByIdAsync(id!.Value);
        Assert.Multiple(() =>
        {
            Assert.That(saved!.Description, Is.Empty);
            Assert.That(output, Does.Contain("Opis: (bez opisa)"));
        });
    }

    [Test]
    public async Task RunAsync_AllowsDescriptionInputToEnd()
    {
        var (id, _) = await RunFormAsync("Predavanje 1\n");

        Assert.That(id, Is.Not.Null);
        Assert.That((await _repository.GetByIdAsync(id!.Value))!.Description, Is.Empty);
    }

    [TestCase("\n")]
    [TestCase("   \n")]
    public async Task RunAsync_AsksAgainForBlankNameThenSaves(string blankName)
    {
        var (id, output) = await RunFormAsync($"{blankName}Predavanje 1\nOpis\n");

        Assert.That(id, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(output, Does.Contain("Naziv materijala je obavezan."));
            Assert.That(CountOccurrences(output, "Naziv (obavezno): "), Is.EqualTo(2));
        });
    }

    [Test]
    public async Task RunAsync_SavesNothingWhenInputEndsWithoutName()
    {
        var (id, output) = await RunFormAsync("   \n");

        Assert.Multiple(() =>
        {
            Assert.That(id, Is.Null);
            Assert.That(output, Does.Contain("Materijal nije kreiran."));
        });
    }

    [Test]
    public async Task RunAsync_SavesNothingWhenInputIsEmpty()
    {
        var (id, output) = await RunFormAsync("");

        Assert.Multiple(() =>
        {
            Assert.That(id, Is.Null);
            Assert.That(output, Does.Contain("Unos je prekinut."));
        });
    }

    [Test]
    public async Task RunAsync_ShowsDenialForUserWhoIsNotStaff()
    {
        var (id, output) = await RunFormAsync("Predavanje 1\nOpis\n", userId: Guid.NewGuid());

        Assert.Multiple(() =>
        {
            Assert.That(id, Is.Null);
            Assert.That(output, Does.Contain("Materijal nije kreiran:"));
            Assert.That(output, Does.Not.Contain("sačuvan"));
        });
    }

    [Test]
    public async Task RunAsync_AcceptsSingleCharacterName()
    {
        var (id, _) = await RunFormAsync("A\n\n");

        Assert.That((await _repository.GetByIdAsync(id!.Value))!.Name, Is.EqualTo("A"));
    }

    [Test]
    public void Constructor_RejectsMissingDependencies()
    {
        var handler = new CreateMaterialHandler(_repository, new InMemoryOfferingStaffChecker());

        Assert.Multiple(() =>
        {
            Assert.That(() => new CreateMaterialForm(null!, TextReader.Null, TextWriter.Null),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => new CreateMaterialForm(handler, null!, TextWriter.Null),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => new CreateMaterialForm(handler, TextReader.Null, null!),
                Throws.TypeOf<ArgumentNullException>());
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
