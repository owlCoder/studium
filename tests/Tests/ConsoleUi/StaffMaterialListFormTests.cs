using System.Text;
using Application.Materials;
using ConsoleUi.Materials;
using Domain.Materials;
using Infrastructure.Materials;
using NUnit.Framework;

namespace Tests.ConsoleUi;

/// <summary>
/// ST-009, US-08, tačka 3: dugme „Objavi” i oznaka stanja u listi materijala (konzola).
/// </summary>
[TestFixture]
public sealed class StaffMaterialListFormTests
{
    private static readonly Guid TeacherId = Guid.NewGuid();
    private static readonly Guid OfferingId = Guid.NewGuid();

    private InMemoryMaterialRepository _materials = null!;
    private InMemoryMaterialAttachmentRepository _attachments = null!;
    private LocalDiskAttachmentFileStorage _storage = null!;
    private string _root = null!;

    [SetUp]
    public void SetUp()
    {
        _materials = new InMemoryMaterialRepository();
        _attachments = new InMemoryMaterialAttachmentRepository();
        _root = Path.Combine(Path.GetTempPath(), "studium-us08-lista-" + Guid.NewGuid().ToString("N"));
        _storage = new LocalDiskAttachmentFileStorage(_root);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private async Task<(bool Published, string Output)> RunFormAsync(string userInput, Guid? userId = null)
    {
        var staffChecker = new InMemoryOfferingStaffChecker([(TeacherId, OfferingId)]);
        var output = new StringWriter();
        var form = new StaffMaterialListForm(
            new ListMaterialsForStaffHandler(_materials, staffChecker),
            new PublishMaterialHandler(_materials, _attachments, _storage, staffChecker),
            new StringReader(userInput),
            output);

        var published = await form.RunAsync(userId ?? TeacherId, OfferingId);
        return (published, output.ToString());
    }

    private async Task<Material> AddAsync(string name, bool publish = false)
    {
        var material = new Material(Guid.NewGuid(), OfferingId, name, null);
        if (publish)
            material.Publish();
        await _materials.AddAsync(material);
        return material;
    }

    private async Task AttachAsync(Material material, string fileName, bool deleteFile = false)
    {
        var key = await _storage.SaveAsync(new MemoryStream(Encoding.UTF8.GetBytes("sadrzaj " + fileName)));
        if (deleteFile)
            await _storage.DeleteAsync(key);

        await _attachments.AddAsync(
            new MaterialAttachment(Guid.NewGuid(), material.Id, fileName, "application/pdf", 10, key));
    }

    private async Task<MaterialPublicationState> StoredStateAsync(Material material) =>
        (await _materials.GetByIdAsync(material.Id))!.PublicationState;

    // --- Oznaka stanja i dugme u listi ---

    [Test]
    public async Task RunAsync_ShowsStateLabelOfEachMaterialAndPublishButtonOnlyForDraft()
    {
        await AddAsync("Predavanje 1", publish: true);
        await AddAsync("Vežbe 1");

        var (_, output) = await RunFormAsync("\n");

        Assert.Multiple(() =>
        {
            Assert.That(output, Does.Contain("Materijali izvođenja (2):"));
            Assert.That(output, Does.Contain("  1. Predavanje 1 — Objavljen" + Environment.NewLine));
            Assert.That(output, Does.Contain("  2. Vežbe 1 — Nacrt [Objavi]"));
        });
    }

    [TestCase(MaterialPublicationState.Draft, "Nacrt")]
    [TestCase(MaterialPublicationState.Published, "Objavljen")]
    [TestCase(MaterialPublicationState.Withdrawn, "Povučen")]
    public void StateLabel_ReturnsLabelForEachState(MaterialPublicationState state, string expected)
    {
        Assert.That(StaffMaterialListForm.StateLabel(state), Is.EqualTo(expected));
    }

    // --- Pozitivno: objava preko dugmeta ---

    [Test]
    public async Task RunAsync_ChoosingDraft_PublishesAndShowsNewState()
    {
        var material = await AddAsync("Vežbe 1");
        await AttachAsync(material, "zadaci.pdf");

        var (published, output) = await RunFormAsync("1\n");

        Assert.Multiple(async () =>
        {
            Assert.That(published, Is.True);
            Assert.That(await StoredStateAsync(material), Is.EqualTo(MaterialPublicationState.Published));
            Assert.That(output, Does.Contain("Materijal „Vežbe 1” je objavljen."));
            Assert.That(output, Does.Contain("  1. Vežbe 1 — Objavljen" + Environment.NewLine));
        });
    }

    [Test]
    public async Task RunAsync_PublishesOnlyChosenMaterial()
    {
        var first = await AddAsync("A materijal");
        var second = await AddAsync("B materijal");

        var (published, _) = await RunFormAsync("2\n");

        Assert.Multiple(async () =>
        {
            Assert.That(published, Is.True);
            Assert.That(await StoredStateAsync(first), Is.EqualTo(MaterialPublicationState.Draft));
            Assert.That(await StoredStateAsync(second), Is.EqualTo(MaterialPublicationState.Published));
        });
    }

    // --- Negativno ---

    [Test]
    public async Task RunAsync_AttachmentWithoutFile_RejectsAndMaterialStaysDraft()
    {
        var material = await AddAsync("Vežbe 1");
        await AttachAsync(material, "zadaci.pdf");
        await AttachAsync(material, "primeri.zip", deleteFile: true);

        var (published, output) = await RunFormAsync("1\n");

        Assert.Multiple(async () =>
        {
            Assert.That(published, Is.False);
            Assert.That(await StoredStateAsync(material), Is.EqualTo(MaterialPublicationState.Draft));
            Assert.That(output, Does.Contain(
                "Objava odbijena: nedostaju datoteke priloga: primeri.zip. Materijal ostaje nacrt."));
        });
    }

    [Test]
    public async Task RunAsync_ChoosingPublishedMaterial_IsRejected()
    {
        await AddAsync("Predavanje 1", publish: true);
        await AddAsync("Vežbe 1");

        var (published, output) = await RunFormAsync("1\n");

        Assert.Multiple(() =>
        {
            Assert.That(published, Is.False);
            Assert.That(output, Does.Contain("Objava odbijena: objaviti se može samo materijal u stanju nacrta."));
        });
    }

    [Test]
    public async Task RunAsync_UserNotStaff_SeesNoListAndNothingIsPublished()
    {
        var material = await AddAsync("Vežbe 1");

        var (published, output) = await RunFormAsync("1\n", userId: Guid.NewGuid());

        Assert.Multiple(async () =>
        {
            Assert.That(published, Is.False);
            Assert.That(output, Does.Contain("Lista materijala nije dostupna:"));
            Assert.That(output, Does.Not.Contain("Vežbe 1"));
            Assert.That(await StoredStateAsync(material), Is.EqualTo(MaterialPublicationState.Draft));
        });
    }

    [TestCase("abc")]
    [TestCase("0")]
    [TestCase("2")]
    [TestCase("-1")]
    public async Task RunAsync_InvalidChoice_PublishesNothing(string choice)
    {
        var material = await AddAsync("Vežbe 1");

        var (published, output) = await RunFormAsync(choice + "\n");

        Assert.Multiple(async () =>
        {
            Assert.That(published, Is.False);
            Assert.That(output, Does.Contain("Neispravan izbor."));
            Assert.That(await StoredStateAsync(material), Is.EqualTo(MaterialPublicationState.Draft));
        });
    }

    // --- Granično ---

    [Test]
    public async Task RunAsync_EmptyChoice_PublishesNothing()
    {
        var material = await AddAsync("Vežbe 1");

        var (published, _) = await RunFormAsync("\n");

        Assert.Multiple(async () =>
        {
            Assert.That(published, Is.False);
            Assert.That(await StoredStateAsync(material), Is.EqualTo(MaterialPublicationState.Draft));
        });
    }

    [Test]
    public async Task RunAsync_NoDrafts_DoesNotAskForChoice()
    {
        await AddAsync("Predavanje 1", publish: true);

        var (published, output) = await RunFormAsync(string.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(published, Is.False);
            Assert.That(output, Does.Not.Contain("[Objavi]"));
            Assert.That(output, Does.Not.Contain("Redni broj materijala"));
        });
    }

    [Test]
    public async Task RunAsync_OfferingWithoutMaterials_ShowsEmptyListWithoutChoice()
    {
        var (published, output) = await RunFormAsync(string.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(published, Is.False);
            Assert.That(output, Does.Contain("Materijali izvođenja (0):"));
            Assert.That(output, Does.Not.Contain("Redni broj materijala"));
        });
    }

    [Test]
    public void Constructor_RejectsNullDependencies()
    {
        var checker = new InMemoryOfferingStaffChecker([]);
        var list = new ListMaterialsForStaffHandler(_materials, checker);
        var publish = new PublishMaterialHandler(_materials, _attachments, _storage, checker);

        Assert.Multiple(() =>
        {
            Assert.That(() => new StaffMaterialListForm(null!, publish, TextReader.Null, TextWriter.Null), Throws.ArgumentNullException);
            Assert.That(() => new StaffMaterialListForm(list, null!, TextReader.Null, TextWriter.Null), Throws.ArgumentNullException);
            Assert.That(() => new StaffMaterialListForm(list, publish, null!, TextWriter.Null), Throws.ArgumentNullException);
            Assert.That(() => new StaffMaterialListForm(list, publish, TextReader.Null, null!), Throws.ArgumentNullException);
        });
    }
}
