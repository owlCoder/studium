using Application.Materials;
using ConsoleUi.Materials;
using Domain.Materials;
using Infrastructure.Access;
using Infrastructure.Materials;
using NUnit.Framework;

namespace Tests.ConsoleUi;

/// <summary>
/// ST-009, US-10, tačka 2: stranica sa listom materijala za studenta (naziv, kratak opis).
/// </summary>
[TestFixture]
public sealed class StudentMaterialListViewTests
{
    private static readonly Guid OfferingId = Guid.NewGuid();
    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid OtherStudentId = Guid.NewGuid();

    private InMemoryMaterialRepository _materials = null!;
    private StringWriter _output = null!;

    [SetUp]
    public void SetUp()
    {
        _materials = new InMemoryMaterialRepository();
        _output = new StringWriter();
    }

    [TearDown]
    public void TearDown() => _output.Dispose();

    private StudentMaterialListView CreateView() =>
        new(
            new ListPublishedMaterialsForStudentHandler(
                _materials,
                new InMemoryMembershipReader(
                [
                    new OfferingMembership(StudentId, OfferingId, MembershipKind.Student, true),
                    new OfferingMembership(OtherStudentId, Guid.NewGuid(), MembershipKind.Student, true)
                ])),
            _output);

    private async Task<Material> AddAsync(string name, string? description, bool publish = true)
    {
        var material = new Material(Guid.NewGuid(), OfferingId, name, description);
        if (publish)
            material.Publish();
        await _materials.AddAsync(material);
        return material;
    }

    private string[] Lines() =>
        _output.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

    // Pozitivno ponašanje

    [Test]
    public async Task ShowAsync_PrintsNameAndDescriptionOfEachPublishedMaterial()
    {
        await AddAsync("Predavanje 1", "Uvod u predmet");
        await AddAsync("Vežbe 1", "Zadaci za vežbu");

        var shown = await CreateView().ShowAsync(StudentId, OfferingId);

        Assert.Multiple(() =>
        {
            Assert.That(shown, Has.Count.EqualTo(2));
            Assert.That(Lines(), Is.EqualTo(new[]
            {
                "Materijali predmeta (2):",
                "  - Predavanje 1",
                "    Uvod u predmet",
                "  - Vežbe 1",
                "    Zadaci za vežbu"
            }));
        });
    }

    [Test]
    public async Task ShowAsync_DoesNotShowDraftMaterial()
    {
        await AddAsync("Predavanje 1", "Objavljeno");
        await AddAsync("Nacrt predavanja", "Još nije objavljeno", publish: false);

        await CreateView().ShowAsync(StudentId, OfferingId);

        Assert.Multiple(() =>
        {
            Assert.That(_output.ToString(), Does.Contain("Materijali predmeta (1):"));
            Assert.That(_output.ToString(), Does.Not.Contain("Nacrt predavanja"));
            Assert.That(_output.ToString(), Does.Not.Contain("Još nije objavljeno"));
        });
    }

    [Test]
    public async Task ShowAsync_LongDescriptionIsShortenedWithEllipsis()
    {
        var description = new string('a', 150);
        await AddAsync("Predavanje 1", description);

        await CreateView().ShowAsync(StudentId, OfferingId);

        Assert.That(Lines()[2], Is.EqualTo("    " + new string('a', 100) + "…"));
    }

    [Test]
    public async Task ShowAsync_MaterialWithoutDescription_ShowsOnlyName()
    {
        await AddAsync("Predavanje 1", null);

        await CreateView().ShowAsync(StudentId, OfferingId);

        Assert.That(Lines(), Is.EqualTo(new[] { "Materijali predmeta (1):", "  - Predavanje 1" }));
    }

    // Negativno ponašanje

    [Test]
    public async Task ShowAsync_StudentOfAnotherOffering_GetsMessageAndNoMaterials()
    {
        await AddAsync("Predavanje 1", "Uvod u predmet");

        var shown = await CreateView().ShowAsync(OtherStudentId, OfferingId);

        Assert.Multiple(() =>
        {
            Assert.That(shown, Is.Null);
            Assert.That(_output.ToString(), Does.StartWith("Lista materijala nije dostupna:"));
            Assert.That(_output.ToString(), Does.Not.Contain("Predavanje 1"));
        });
    }

    [Test]
    public async Task ShowAsync_EmptyUser_GetsMessage()
    {
        await AddAsync("Predavanje 1", "Uvod u predmet");

        var shown = await CreateView().ShowAsync(Guid.Empty, OfferingId);

        Assert.Multiple(() =>
        {
            Assert.That(shown, Is.Null);
            Assert.That(_output.ToString(), Does.StartWith("Lista materijala nije dostupna:"));
        });
    }

    [Test]
    public void Constructor_RejectsMissingDependencies()
    {
        var handler = new ListPublishedMaterialsForStudentHandler(
            _materials,
            new InMemoryMembershipReader([]));

        Assert.Multiple(() =>
        {
            Assert.That(() => new StudentMaterialListView(null!, _output), Throws.ArgumentNullException);
            Assert.That(() => new StudentMaterialListView(handler, null!), Throws.ArgumentNullException);
        });
    }

    // Granično ponašanje: kratak opis

    [Test]
    public void ShortenDescription_ExactlyMaxLength_IsNotShortened()
    {
        var description = new string('b', StudentMaterialListView.ShortDescriptionMaxLength);

        Assert.That(StudentMaterialListView.ShortenDescription(description), Is.EqualTo(description));
    }

    [Test]
    public void ShortenDescription_OneOverMaxLength_IsShortened()
    {
        var description = new string('b', StudentMaterialListView.ShortDescriptionMaxLength + 1);

        Assert.That(
            StudentMaterialListView.ShortenDescription(description),
            Is.EqualTo(new string('b', StudentMaterialListView.ShortDescriptionMaxLength) + "…"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("\r\n\t")]
    public void ShortenDescription_EmptyDescription_ReturnsEmpty(string? description)
    {
        Assert.That(StudentMaterialListView.ShortenDescription(description), Is.Empty);
    }

    [Test]
    public void ShortenDescription_MultiLineDescription_BecomesOneLine()
    {
        Assert.That(
            StudentMaterialListView.ShortenDescription("Prvi red\r\nDrugi   red\n\nTreći"),
            Is.EqualTo("Prvi red Drugi red Treći"));
    }

    [Test]
    public void ShortenDescription_CutBeforeSpace_DoesNotLeaveTrailingSpace()
    {
        var description = new string('c', 99) + " nastavak opisa";

        Assert.That(
            StudentMaterialListView.ShortenDescription(description),
            Is.EqualTo(new string('c', 99) + "…"));
    }

    [Test]
    public void ShortenDescription_DoesNotSplitTwoPartCharacter()
    {
        var description = new string('d', 99) + "😀" + "kraj";

        var shortened = StudentMaterialListView.ShortenDescription(description);

        Assert.Multiple(() =>
        {
            Assert.That(shortened, Is.EqualTo(new string('d', 99) + "…"));
            Assert.That(char.IsHighSurrogate(shortened[^2]), Is.False);
        });
    }

    [Test]
    public void ShortenDescription_KeepsSerbianLetters()
    {
        Assert.That(
            StudentMaterialListView.ShortenDescription("Čitanje: ćirilica, šđž"),
            Is.EqualTo("Čitanje: ćirilica, šđž"));
    }

    // Tačka 3: prazno stanje

    [Test]
    public async Task ShowAsync_OfferingWithoutMaterials_ShowsEmptyState()
    {
        var shown = await CreateView().ShowAsync(StudentId, OfferingId);

        Assert.Multiple(() =>
        {
            Assert.That(shown, Is.Empty);
            Assert.That(Lines(), Is.EqualTo(new[]
            {
                "Materijali predmeta (0):",
                StudentMaterialListView.EmptyStateMessage
            }));
        });
    }

    [Test]
    public async Task ShowAsync_OnlyDrafts_ShowsSameEmptyStateWithoutRevealingDrafts()
    {
        await AddAsync("Tajni nacrt", "Opis nacrta", publish: false);
        await AddAsync("Drugi nacrt", null, publish: false);

        var shown = await CreateView().ShowAsync(StudentId, OfferingId);

        Assert.Multiple(() =>
        {
            Assert.That(shown, Is.Empty);
            Assert.That(Lines(), Is.EqualTo(new[]
            {
                "Materijali predmeta (0):",
                StudentMaterialListView.EmptyStateMessage
            }));
            Assert.That(_output.ToString(), Does.Not.Contain("nacrt").IgnoreCase);
        });
    }

    [Test]
    public async Task ShowAsync_OnlyMaterialsOfAnotherOffering_ShowsEmptyState()
    {
        var foreign = new Material(Guid.NewGuid(), Guid.NewGuid(), "Tuđe predavanje", "Opis");
        foreign.Publish();
        await _materials.AddAsync(foreign);

        await CreateView().ShowAsync(StudentId, OfferingId);

        Assert.Multiple(() =>
        {
            Assert.That(_output.ToString(), Does.Contain(StudentMaterialListView.EmptyStateMessage));
            Assert.That(_output.ToString(), Does.Not.Contain("Tuđe predavanje"));
        });
    }

    [Test]
    public async Task ShowAsync_WithMaterials_DoesNotShowEmptyState()
    {
        await AddAsync("Predavanje 1", "Uvod u predmet");

        await CreateView().ShowAsync(StudentId, OfferingId);

        Assert.That(_output.ToString(), Does.Not.Contain(StudentMaterialListView.EmptyStateMessage));
    }

    [Test]
    public async Task ShowAsync_OnePublishedAmongDrafts_ShowsItAndNoEmptyState()
    {
        await AddAsync("Nacrt", "Nije objavljen", publish: false);
        await AddAsync("Predavanje 1", "Uvod u predmet");

        await CreateView().ShowAsync(StudentId, OfferingId);

        Assert.That(Lines(), Is.EqualTo(new[]
        {
            "Materijali predmeta (1):",
            "  - Predavanje 1",
            "    Uvod u predmet"
        }));
    }

    [Test]
    public async Task ShowAsync_UserWithoutRights_GetsDenialInsteadOfEmptyState()
    {
        var shown = await CreateView().ShowAsync(OtherStudentId, OfferingId);

        Assert.Multiple(() =>
        {
            Assert.That(shown, Is.Null);
            Assert.That(_output.ToString(), Does.StartWith("Lista materijala nije dostupna:"));
            Assert.That(_output.ToString(), Does.Not.Contain(StudentMaterialListView.EmptyStateMessage));
            Assert.That(_output.ToString(), Does.Not.Contain("Materijali predmeta"));
        });
    }

    [Test]
    public async Task ShowAsync_OnlyWithdrawnMaterial_ShowsEmptyStateWithoutRevealingIt()
    {
        var withdrawn = await AddAsync("Povučeno predavanje", "Više nije dostupno");
        withdrawn.Withdraw();
        await _materials.UpdateAsync(withdrawn);

        await CreateView().ShowAsync(StudentId, OfferingId);

        Assert.Multiple(() =>
        {
            Assert.That(Lines(), Is.EqualTo(new[]
            {
                "Materijali predmeta (0):",
                StudentMaterialListView.EmptyStateMessage
            }));
            Assert.That(_output.ToString(), Does.Not.Contain("Povučeno predavanje"));
        });
    }

    [Test]
    public async Task ShowAsync_EmptyStateDoesNotChangeMaterials()
    {
        var draft = await AddAsync("Nacrt", null, publish: false);

        await CreateView().ShowAsync(StudentId, OfferingId);

        var stored = await _materials.GetByIdAsync(draft.Id);
        Assert.That(stored!.PublicationState, Is.EqualTo(MaterialPublicationState.Draft));
    }
}
