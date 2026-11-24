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

namespace Tests;

/// <summary>
/// ST-009, „Zaštita pristupa materijalu i prilozima”, tačka 1, preko HTTP-a:
/// GET /materials/{materialId}/attachments/{attachmentId}.
/// </summary>
[TestFixture]
public sealed class MaterialAttachmentDownloadApiTests
{
    private static readonly Guid OfferingId = Guid.NewGuid();
    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid OtherStudentId = Guid.NewGuid();
    private static readonly byte[] FileBytes = Encoding.UTF8.GetBytes("sadrzaj skripte");

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private Material _material = null!;
    private MaterialAttachment _attachment = null!;

    [SetUp]
    public async Task SetUp()
    {
        var materials = new InMemoryMaterialRepository();
        var attachments = new InMemoryMaterialAttachmentRepository();
        var storage = new DownloadMaterialAttachmentHandlerTests.InMemoryAttachmentFileStorage();

        _material = new Material(Guid.NewGuid(), OfferingId, "Predavanje 1", null);
        await materials.AddAsync(_material);
        var key = await storage.SaveAsync(new MemoryStream(FileBytes));
        _attachment = new MaterialAttachment(
            Guid.NewGuid(), _material.Id, "skripta.pdf", "application/pdf", FileBytes.Length, key);
        await attachments.AddAsync(_attachment);

        var memberships = new InMemoryMembershipReader(
        [
            new OfferingMembership(StudentId, OfferingId, MembershipKind.Student, true),
            new OfferingMembership(OtherStudentId, Guid.NewGuid(), MembershipKind.Student, true)
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

    private string Url(Guid? attachmentId = null, Guid? materialId = null) =>
        $"/materials/{materialId ?? _material.Id}/attachments/{attachmentId ?? _attachment.Id}";

    private void LoginAs(Guid userId) =>
        _client.DefaultRequestHeaders.Add(DemoAuthHandler.UserHeader, userId.ToString());

    [Test]
    public async Task Get_WithoutUser_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync(Url());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Get_EnrolledStudent_ReceivesFileWithOriginalNameAndType()
    {
        LoginAs(StudentId);

        var response = await _client.GetAsync(Url());

        Assert.Multiple(async () =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content.Headers.ContentType!.MediaType, Is.EqualTo("application/pdf"));
            Assert.That(response.Content.Headers.ContentDisposition!.FileName, Is.EqualTo("skripta.pdf"));
            Assert.That(await response.Content.ReadAsByteArrayAsync(), Is.EqualTo(FileBytes));
        });
    }

    [Test]
    public async Task Get_StudentOfAnotherOffering_IsForbiddenAndGetsNoContent()
    {
        LoginAs(OtherStudentId);

        var response = await _client.GetAsync(Url());
        var body = await response.Content.ReadAsByteArrayAsync();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            Assert.That(body, Is.Not.EqualTo(FileBytes));
        });
    }

    [Test]
    public async Task Get_UnknownAttachment_ForEnrolledStudent_ReturnsNotFound()
    {
        LoginAs(StudentId);

        var response = await _client.GetAsync(Url(Guid.NewGuid()));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Get_UnknownMaterial_ReturnsNotFound()
    {
        LoginAs(StudentId);

        var response = await _client.GetAsync(Url(materialId: Guid.NewGuid()));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
