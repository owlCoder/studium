using System.Net;
using System.Text;
using Api.Authentication;
using Application.Access;
using Application.Materials;
using Domain.Materials;
using Infrastructure.Access;
using Infrastructure.Materials;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Tests.Application;

namespace Tests.Api;

/// <summary>
/// ST-009, „Zaštita pristupa materijalu i prilozima”, tačka 4:
/// student drugog izvođenja ne može otvoriti materijal.
/// Scenario: isti predmet (ERS) u dve akademske godine. Ana je aktivan student izvođenja
/// 2026/27 i pokušava da otvori materijal izvođenja 2025/26, iako je on objavljen.
/// Članstvo u jednom izvođenju ne daje pristup drugom izvođenju istog predmeta.
/// Pokrivaju se ulazi kojima student danas dolazi do materijala: lista materijala
/// izvođenja (US-10) i direktna adresa priloga (tačke 1–2 ove priče).
/// </summary>
[TestFixture]
public sealed class MaterialOtherOfferingAccessApiTests
{
    private static readonly Guid Ers2026 = Guid.NewGuid();
    private static readonly Guid Ers2025 = Guid.NewGuid();
    private static readonly Guid AnaId = Guid.NewGuid();     // aktivna studentkinja ERS 2026/27
    private static readonly Guid PetarId = Guid.NewGuid();   // aktivan student ERS 2025/26
    private static readonly byte[] FileBytes = Encoding.UTF8.GetBytes("sadrzaj predavanja");

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private Material _material2026 = null!;
    private MaterialAttachment _attachment2026 = null!;
    private Material _material2025 = null!;
    private MaterialAttachment _attachment2025 = null!;

    [SetUp]
    public async Task SetUp()
    {
        var materials = new InMemoryMaterialRepository();
        var attachments = new InMemoryMaterialAttachmentRepository();
        var storage = new DownloadMaterialAttachmentHandlerTests.InMemoryAttachmentFileStorage();

        (_material2026, _attachment2026) = await AddPublishedAsync(
            materials, attachments, storage, Ers2026, "Predavanje 1 (2026/27)", "uvod-2026.pdf");
        (_material2025, _attachment2025) = await AddPublishedAsync(
            materials, attachments, storage, Ers2025, "Predavanje 1 (2025/26)", "uvod-2025.pdf");

        var memberships = new InMemoryMembershipReader(
        [
            new OfferingMembership(AnaId, Ers2026, MembershipKind.Student, true),
            new OfferingMembership(PetarId, Ers2025, MembershipKind.Student, true)
        ]);

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IMaterialRepository>(materials);
                services.AddSingleton<IMaterialAttachmentRepository>(attachments);
                services.AddSingleton<IAttachmentFileStorage>(storage);
                services.AddSingleton<IMembershipReader>(memberships);
            }));
        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private static async Task<(Material, MaterialAttachment)> AddPublishedAsync(
        InMemoryMaterialRepository materials,
        InMemoryMaterialAttachmentRepository attachments,
        IAttachmentFileStorage storage,
        Guid offeringId,
        string name,
        string fileName)
    {
        var material = new Material(Guid.NewGuid(), offeringId, name, "Opis");
        material.Publish();
        await materials.AddAsync(material);

        var key = await storage.SaveAsync(new MemoryStream(FileBytes));
        var attachment = new MaterialAttachment(
            Guid.NewGuid(), material.Id, fileName, "application/pdf", FileBytes.Length, key);
        await attachments.AddAsync(attachment);
        return (material, attachment);
    }

    private void LoginAs(Guid userId) =>
        _client.DefaultRequestHeaders.Add(DemoAuthHandler.UserHeader, userId.ToString());

    private static string ListUrl(Guid offeringId) => $"/offerings/{offeringId}/materials";

    private static string DownloadUrl(Material material, MaterialAttachment attachment) =>
        $"/materials/{material.Id}/attachments/{attachment.Id}";

    // --- Kontrola: svoje izvođenje radi ---

    [Test]
    public async Task OwnOffering_ListShowsOnlyOwnOfferingMaterial()
    {
        LoginAs(AnaId);

        var response = await _client.GetAsync(ListUrl(Ers2026));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(body, Does.Contain(_material2026.Name));
            Assert.That(body, Does.Not.Contain(_material2025.Name),
                "Lista svog izvođenja ne sme da sadrži materijal drugog izvođenja istog predmeta.");
        });
    }

    [Test]
    public async Task OwnOffering_AttachmentCanBeDownloaded()
    {
        LoginAs(AnaId);

        var response = await _client.GetAsync(DownloadUrl(_material2026, _attachment2026));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    // --- Tačka 4: drugo izvođenje istog predmeta ---

    [Test]
    public async Task OtherOffering_ListIsForbiddenAndRevealsNoMaterial()
    {
        LoginAs(AnaId);

        var response = await _client.GetAsync(ListUrl(Ers2025));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            Assert.That(body, Does.Not.Contain(_material2025.Name));
            Assert.That(body, Does.Not.Contain(_material2025.Id.ToString()));
        });
    }

    [Test]
    public async Task OtherOffering_AttachmentByDirectAddressIsForbiddenWithoutFileTrace()
    {
        LoginAs(AnaId);

        var response = await _client.GetAsync(DownloadUrl(_material2025, _attachment2025));
        var body = await response.Content.ReadAsByteArrayAsync();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            Assert.That(body, Is.Not.EqualTo(FileBytes));
            Assert.That(response.Content.Headers.ContentDisposition, Is.Null);
            Assert.That(Encoding.UTF8.GetString(body), Does.Not.Contain(_attachment2025.OriginalFileName));
        });
    }

    [Test]
    public async Task OtherOffering_AttachmentThroughOwnMaterialAddressIsNotServed()
    {
        // Ana ima pravo nad svojim materijalom, pa pokušava da prilog drugog izvođenja
        // „provuče” kroz adresu svog materijala.
        LoginAs(AnaId);

        var response = await _client.GetAsync(DownloadUrl(_material2026, _attachment2025));
        var body = await response.Content.ReadAsByteArrayAsync();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(body, Is.Not.EqualTo(FileBytes));
            Assert.That(response.Content.Headers.ContentDisposition, Is.Null);
        });
    }

    [Test]
    public async Task OtherOffering_DeniedInBothDirections()
    {
        // Pravilo važi simetrično: ni student 2025/26 ne otvara materijal 2026/27.
        LoginAs(PetarId);

        var list = await _client.GetAsync(ListUrl(Ers2026));
        var download = await _client.GetAsync(DownloadUrl(_material2026, _attachment2026));

        Assert.Multiple(() =>
        {
            Assert.That(list.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            Assert.That(download.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        });
    }
}
