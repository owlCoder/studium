namespace Tests.Infrastructure;

using NUnit.Framework;
using Domain;
using Application;
using global::Infrastructure;

[TestFixture]
public class SubmissionOverviewServiceTests
{
    private MockSubmissionOverviewService _service = null!;
    private Guid _authorizedMemberId;
    private Guid _unauthorizedMemberId;
    private Guid _validTaskId;

    [SetUp]
    public void Setup()
    {
        _service = new MockSubmissionOverviewService();
        _authorizedMemberId = _service.DefaultMemberId; // Koristi kreirani autorizovani ID
        _validTaskId = _service.DefaultTaskId;           // Koristi kreirani validni Task ID
        _unauthorizedMemberId = Guid.NewGuid();          // Različit ID koji nije u listi autorizovanih
    }

    [Test]
    public async Task GetSubmissionOverviewAsync_NonExistingId_ReturnsNull()
    {
        var nonExistingId = Guid.NewGuid();
        var result = await _service.GetSubmissionOverviewAsync(nonExistingId, _authorizedMemberId);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetSubmissionOverviewAsync_UnauthorizedUser_ThrowsException()
    {
        var submissionId = Guid.NewGuid();
        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
        {
            await _service.GetSubmissionOverviewAsync(submissionId, _unauthorizedMemberId);
        });
    }

    [Test]
    public async Task GetStatusCountsForTaskAsync_ReturnsCounts()
    {
        var counts = await _service.GetStatusCountsForTaskAsync(_validTaskId, _authorizedMemberId);
        
        Assert.Multiple(() =>
        {
            Assert.That(counts.TotalCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(counts.PendingCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task GetStatusCountsForTaskAsync_BrojiStudenteBezPredaje()
    {
        var counts = await _service.GetStatusCountsForTaskAsync(_validTaskId, _authorizedMemberId);

        Assert.Multiple(() =>
        {
            Assert.That(counts.WithoutSubmissionCount, Is.EqualTo(1));
            Assert.That(counts.UnreviewedCount, Is.EqualTo(1));
            Assert.That(counts.ReviewedCount, Is.EqualTo(1));
            Assert.That(counts.AcceptedCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task GetStatusCountsForTaskAsync_TotalJednakDuziniListePredaja()
    {
        var list = await _service.GetSubmissionsForTaskAsync(_validTaskId, _authorizedMemberId);
        var counts = await _service.GetStatusCountsForTaskAsync(_validTaskId, _authorizedMemberId);

        Assert.Multiple(() =>
        {
            Assert.That(counts.TotalCount, Is.EqualTo(list.Count));
            Assert.That(counts.UnreviewedCount, Is.EqualTo(list.Count(s => s.Status == global::Domain.SubmissionStatus.Pending)));
            Assert.That(counts.ReviewedCount, Is.EqualTo(list.Count(s => s.Status != global::Domain.SubmissionStatus.Pending)));
        });
    }

    [Test]
    public void GetStatusCountsForTaskAsync_NeovlascenKorisnik_BacaIzuzetak()
    {
        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.GetStatusCountsForTaskAsync(_validTaskId, _unauthorizedMemberId));
    }
}