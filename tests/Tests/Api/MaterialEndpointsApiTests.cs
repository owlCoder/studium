using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Api.Authentication;
using Application.Materials;
using Infrastructure.Materials;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Tests.Api;

/// <summary>
/// ST-009: HTTP ugovor endpointa za materijale i priloge (statusi i telo odgovora),
/// preko celog API-ja sa demo prijavom tima ST-006 (zaglavlje X-Demo-UserId).
/// Provera osoblja (ST-005) i skladište datoteka zamenjeni su testnim adapterima.
/// </summary>
[TestFixture]
public sealed class MaterialEndpointsApiTests
{
    private static readonly Guid TeacherId = Guid.Parse("7e7e7e7e-0000-0000-0000-000000000001");
    private static readonly Guid OtherUserId = Guid.Parse("7e7e7e7e-0000-0000-0000-000000000002");
    private static readonly Guid OfferingId = Guid.Parse("0ff0ff0f-0000-0000-0000-000000000001");
    private static readonly Guid OtherOfferingId = Guid.Parse("0ff0ff0f-0000-0000-0000-000000000002");

    private string _storageRoot = null!;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _storageRoot = Path.Combine(Path.GetTempPath(), "studium-api-tests-" + Guid.NewGuid().ToString("N"));
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IOfferingStaffChecker>(
                    new InMemoryOfferingStaffChecker([(TeacherId, OfferingId)]));
                services.AddSingleton<IAttachmentFileStorage>(
                    new LocalDiskAttachmentFileStorage(_storageRoot));
            }));
        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
        if (Directory.Exists(_storageRoot))
            Directory.Delete(_storageRoot, recursive: true);
    }

    private HttpRequestMessage Request(HttpMethod method, string url, Guid? userId, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, url) { Content = content };
        if (userId is not null)
            request.Headers.Add(DemoAuthHandler.UserHeader, userId.Value.ToString());
        return request;
    }

    private Task<HttpResponseMessage> CreateMaterialAsync(Guid? userId, Guid offeringId, string? name) =>
        _client.SendAsync(Request(
            HttpMethod.Post, $"/offerings/{offeringId}/materials", userId,
            JsonContent.Create(new { name, description = "Opis" })));

    private async Task<Guid> CreateMaterialAsTeacherAsync()
    {
        var response = await CreateMaterialAsync(TeacherId, OfferingId, "Predavanje 1");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        using var body = await ReadJsonAsync(response);
        return body.RootElement.GetProperty("id").GetGuid();
    }

    private static MultipartFormDataContent Files(params (string Name, string Text)[] files)
    {
        var content = new MultipartFormDataContent();
        foreach (var (name, text) in files)
        {
            var part = new ByteArrayContent(Encoding.UTF8.GetBytes(text));
            part.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            content.Add(part, "files", name);
        }
        return content;
    }

    private Task<HttpResponseMessage> UploadAsync(Guid? userId, Guid materialId, MultipartFormDataContent content) =>
        _client.SendAsync(Request(HttpMethod.Post, $"/materials/{materialId}/attachments", userId, content));

    private Task<HttpResponseMessage> RemoveAsync(Guid? userId, Guid materialId, Guid attachmentId) =>
        _client.SendAsync(Request(
            HttpMethod.Delete, $"/materials/{materialId}/attachments/{attachmentId}", userId));

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync());

    private static async Task<string> ValidationErrorAsync(HttpResponseMessage response, string key)
    {
        using var body = await ReadJsonAsync(response);
        return body.RootElement.GetProperty("errors").GetProperty(key)[0].GetString()!;
    }

    // Kreiranje materijala: POST /offerings/{offeringId}/materials

    [Test]
    public async Task CreateMaterial_WithoutUser_Returns401()
    {
        var response = await CreateMaterialAsync(null, OfferingId, "Predavanje 1");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task CreateMaterial_UserNotStaffOfOffering_Returns403()
    {
        var response = await CreateMaterialAsync(TeacherId, OtherOfferingId, "Predavanje 1");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        using var body = await ReadJsonAsync(response);
        Assert.That(body.RootElement.GetProperty("detail").GetString(),
            Is.EqualTo("Samo nastavno osoblje izvođenja može kreirati materijal."));
    }

    [Test]
    public async Task CreateMaterial_EmptyName_Returns400WithCleanMessageUnderName()
    {
        var response = await CreateMaterialAsync(TeacherId, OfferingId, "   ");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(await ValidationErrorAsync(response, "name"), Is.EqualTo("Naziv materijala je obavezan."));
    }

    [Test]
    public async Task CreateMaterial_Staff_Returns201WithDraftAndLocation()
    {
        var response = await CreateMaterialAsync(TeacherId, OfferingId, "Predavanje 1");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        using var body = await ReadJsonAsync(response);
        var id = body.RootElement.GetProperty("id").GetGuid();
        Assert.Multiple(() =>
        {
            Assert.That(body.RootElement.GetProperty("publicationState").GetString(), Is.EqualTo("Draft"));
            Assert.That(response.Headers.Location!.ToString(), Is.EqualTo($"/offerings/{OfferingId}/materials/{id}"));
        });
    }

    // Otpremanje: POST /materials/{materialId}/attachments

    [Test]
    public async Task Upload_WithoutUser_Returns401()
    {
        var materialId = await CreateMaterialAsTeacherAsync();

        var response = await UploadAsync(null, materialId, Files(("a.pdf", "x")));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Upload_UnknownMaterial_Returns404()
    {
        var response = await UploadAsync(TeacherId, Guid.NewGuid(), Files(("a.pdf", "x")));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Upload_UserNotStaff_Returns403()
    {
        var materialId = await CreateMaterialAsTeacherAsync();

        var response = await UploadAsync(OtherUserId, materialId, Files(("a.pdf", "x")));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Upload_UnsupportedType_Returns400UnderFiles()
    {
        var materialId = await CreateMaterialAsTeacherAsync();

        var response = await UploadAsync(TeacherId, materialId, Files(("a.pdf", "x"), ("program.exe", "y")));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(await ValidationErrorAsync(response, "files"),
            Does.StartWith("Datoteka 'program.exe' nije priložena: tip datoteke nije dozvoljen."));
    }

    [Test]
    public async Task Upload_NoFiles_Returns400WithCleanMessageUnderFiles()
    {
        var materialId = await CreateMaterialAsTeacherAsync();
        var content = new MultipartFormDataContent { { new StringContent("x"), "napomena" } };

        var response = await UploadAsync(TeacherId, materialId, content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(await ValidationErrorAsync(response, "files"),
            Is.EqualTo("Potrebno je poslati bar jednu datoteku."));
    }

    [Test]
    public async Task Upload_Staff_Returns201WithResultPerFile()
    {
        var materialId = await CreateMaterialAsTeacherAsync();

        var response = await UploadAsync(TeacherId, materialId, Files(("1.pdf", "a"), ("2.pdf", "bb")));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        using var body = await ReadJsonAsync(response);
        var results = body.RootElement.EnumerateArray().ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(results.Select(r => r.GetProperty("originalFileName").GetString()),
                Is.EqualTo(new[] { "1.pdf", "2.pdf" }));
            Assert.That(results.All(r => r.GetProperty("saved").GetBoolean()), Is.True);
            Assert.That(results[1].GetProperty("attachment").GetProperty("sizeInBytes").GetInt64(), Is.EqualTo(2));
        });
    }

    // Uklanjanje: DELETE /materials/{materialId}/attachments/{attachmentId}

    private async Task<(Guid MaterialId, Guid AttachmentId)> UploadOneAsTeacherAsync()
    {
        var materialId = await CreateMaterialAsTeacherAsync();
        var response = await UploadAsync(TeacherId, materialId, Files(("a.pdf", "x")));
        using var body = await ReadJsonAsync(response);
        var attachmentId = body.RootElement[0].GetProperty("attachment").GetProperty("id").GetGuid();
        return (materialId, attachmentId);
    }

    [Test]
    public async Task Remove_WithoutUser_Returns401()
    {
        var (materialId, attachmentId) = await UploadOneAsTeacherAsync();

        var response = await RemoveAsync(null, materialId, attachmentId);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Remove_UserNotStaff_Returns403()
    {
        var (materialId, attachmentId) = await UploadOneAsTeacherAsync();

        var response = await RemoveAsync(OtherUserId, materialId, attachmentId);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Remove_Staff_Returns204ThenSecondRemoveReturns404()
    {
        var (materialId, attachmentId) = await UploadOneAsTeacherAsync();

        var first = await RemoveAsync(TeacherId, materialId, attachmentId);
        var second = await RemoveAsync(TeacherId, materialId, attachmentId);

        Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(Directory.GetFiles(_storageRoot), Is.Empty);
    }

    [Test]
    public async Task Remove_UnknownMaterial_Returns404()
    {
        var response = await RemoveAsync(TeacherId, Guid.NewGuid(), Guid.NewGuid());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
