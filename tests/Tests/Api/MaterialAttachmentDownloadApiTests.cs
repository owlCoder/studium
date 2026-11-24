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
/// ST-009, „Zaštita pristupa materijalu i prilozima”, tačka 1, preko HTTP-a:
/// GET /materials/{materialId}/attachments/{attachmentId}.
/// </summary>
[TestFixture]
public sealed class MaterialAttachmentDownloadApiTests
{
    private static readonly Guid OfferingId = Guid.NewGuid();
    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid OtherStudentId = Guid.NewGuid();
    private static readonly Guid TeacherId = Guid.NewGuid();
    private static readonly Guid NeverEnrolledStudentId = Guid.NewGuid();
    private static readonly Guid EndedStudentId = Guid.NewGuid();
    private static readonly Guid LeavingStudentId = Guid.NewGuid();
    private static readonly byte[] FileBytes = Encoding.UTF8.GetBytes("sadrzaj skripte");

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private Material _material = null!;
    private MaterialAttachment _attachment = null!;
    private Material _draft = null!;
    private MaterialAttachment _draftAttachment = null!;
    private ChangeableMembershipReader _memberships = null!;

    [SetUp]
    public async Task SetUp()
    {
        var materials = new InMemoryMaterialRepository();
        var attachments = new InMemoryMaterialAttachmentRepository();
        var storage = new DownloadMaterialAttachmentHandlerTests.InMemoryAttachmentFileStorage();

        _material = new Material(Guid.NewGuid(), OfferingId, "Predavanje 1", null);
        _material.Publish();
        await materials.AddAsync(_material);
        var key = await storage.SaveAsync(new MemoryStream(FileBytes));
        _attachment = new MaterialAttachment(
            Guid.NewGuid(), _material.Id, "skripta.pdf", "application/pdf", FileBytes.Length, key);
        await attachments.AddAsync(_attachment);

        _draft = new Material(Guid.NewGuid(), OfferingId, "Nacrt vežbi", null);
        await materials.AddAsync(_draft);
        var draftKey = await storage.SaveAsync(new MemoryStream(FileBytes));
        _draftAttachment = new MaterialAttachment(
            Guid.NewGuid(), _draft.Id, "nacrt.pdf", "application/pdf", FileBytes.Length, draftKey);
        await attachments.AddAsync(_draftAttachment);

        // NeverEnrolledStudentId namerno nema nijedan zapis o članstvu.
        _memberships = new ChangeableMembershipReader(
        [
            new OfferingMembership(StudentId, OfferingId, MembershipKind.Student, true),
            new OfferingMembership(OtherStudentId, Guid.NewGuid(), MembershipKind.Student, true),
            new OfferingMembership(EndedStudentId, OfferingId, MembershipKind.Student, false),
            new OfferingMembership(LeavingStudentId, OfferingId, MembershipKind.Student, true)
        ]);
        var memberships = _memberships;

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IMaterialRepository>(materials);
                services.AddSingleton<IMaterialAttachmentRepository>(attachments);
                services.AddSingleton<IAttachmentFileStorage>(storage);
                services.AddSingleton<IMembershipReader>(memberships);
                services.AddSingleton<IOfferingStaffChecker>(
                    new InMemoryOfferingStaffChecker([(TeacherId, OfferingId)]));
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

    // --- Tačka 2: nacrt nedostupan studentu i preko direktne adrese ---

    [Test]
    public async Task Get_EnrolledStudent_DraftByDirectAddress_ReturnsNotFoundWithoutContent()
    {
        LoginAs(StudentId);

        var response = await _client.GetAsync(Url(_draftAttachment.Id, _draft.Id));
        var body = await response.Content.ReadAsByteArrayAsync();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(body, Is.Not.EqualTo(FileBytes));
        });
    }

    [Test]
    public async Task Get_Teacher_DraftAttachment_ReceivesFile()
    {
        LoginAs(TeacherId);

        var response = await _client.GetAsync(Url(_draftAttachment.Id, _draft.Id));

        Assert.Multiple(async () =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(await response.Content.ReadAsByteArrayAsync(), Is.EqualTo(FileBytes));
        });
    }

    // --- Tačka 3: student bez članstva ne može preuzeti prilog direktnom adresom ---
    // Student zna tačnu adresu priloga objavljenog materijala (npr. dobio ju je od kolege).

    private async Task AssertDeniedWithoutAnyFileTraceAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsByteArrayAsync();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            Assert.That(body, Is.Not.EqualTo(FileBytes));
            Assert.That(response.Content.Headers.ContentDisposition, Is.Null,
                "Odbijen odgovor ne sme da otkrije izvorni naziv datoteke.");
            Assert.That(Encoding.UTF8.GetString(body), Does.Not.Contain(_attachment.OriginalFileName));
        });
    }

    [Test]
    public async Task Get_StudentNeverEnrolled_DirectAddress_IsForbiddenWithoutFileTrace()
    {
        LoginAs(NeverEnrolledStudentId);

        var response = await _client.GetAsync(Url());

        await AssertDeniedWithoutAnyFileTraceAsync(response);
    }

    [Test]
    public async Task Get_StudentWithEndedMembership_DirectAddress_IsForbiddenWithoutFileTrace()
    {
        LoginAs(EndedStudentId);

        var response = await _client.GetAsync(Url());

        await AssertDeniedWithoutAnyFileTraceAsync(response);
    }

    [Test]
    public async Task Get_StudentOfAnotherOffering_DirectAddress_IsForbiddenWithoutFileTrace()
    {
        LoginAs(OtherStudentId);

        var response = await _client.GetAsync(Url());

        await AssertDeniedWithoutAnyFileTraceAsync(response);
    }

    [Test]
    public async Task Get_StudentLosesMembership_SameAddressStopsWorkingImmediately()
    {
        LoginAs(LeavingStudentId);

        var first = await _client.GetAsync(Url());
        _memberships.End(LeavingStudentId, OfferingId);
        var second = await _client.GetAsync(Url());

        Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        await AssertDeniedWithoutAnyFileTraceAsync(second);
    }

    [Test]
    public async Task Get_StudentWithoutMembership_UnknownAttachment_GetsSameDenialAsExistingOne()
    {
        // Student bez članstva ne sme pogađanjem adresa da sazna koji prilozi postoje.
        LoginAs(NeverEnrolledStudentId);

        var existing = await _client.GetAsync(Url());
        var unknown = await _client.GetAsync(Url(Guid.NewGuid()));

        Assert.Multiple(() =>
        {
            Assert.That(existing.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            Assert.That(unknown.StatusCode, Is.EqualTo(existing.StatusCode));
        });
    }

    [Test]
    public async Task Get_StudentWithoutMembership_DraftAddress_IsForbidden()
    {
        LoginAs(NeverEnrolledStudentId);

        var response = await _client.GetAsync(Url(_draftAttachment.Id, _draft.Id));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    /// <summary>
    /// Članstva koja test može da promeni između dva zahteva (npr. prekid upisa).
    /// </summary>
    private sealed class ChangeableMembershipReader : IMembershipReader
    {
        private readonly List<OfferingMembership> _memberships;

        public ChangeableMembershipReader(IEnumerable<OfferingMembership> memberships) =>
            _memberships = memberships.ToList();

        public void End(Guid userId, Guid offeringId)
        {
            lock (_memberships)
            {
                var index = _memberships.FindIndex(m => m.UserId == userId && m.OfferingId == offeringId);
                _memberships[index] = _memberships[index] with { IsActive = false };
            }
        }

        public Task<bool> IsActiveStudentAsync(Guid userId, Guid offeringId, CancellationToken cancellationToken = default)
        {
            lock (_memberships)
            {
                return Task.FromResult(_memberships.Any(m =>
                    m.UserId == userId && m.OfferingId == offeringId &&
                    m.Kind == MembershipKind.Student && m.IsActive));
            }
        }

        public Task<bool> IsEngagedStaffAsync(Guid userId, Guid offeringId, CancellationToken cancellationToken = default)
        {
            lock (_memberships)
            {
                return Task.FromResult(_memberships.Any(m =>
                    m.UserId == userId && m.OfferingId == offeringId &&
                    m.Kind is MembershipKind.Teacher or MembershipKind.Assistant && m.IsActive));
            }
        }
    }
}
