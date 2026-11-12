using Application.Submissions;
using Infrastructure;
using NUnit.Framework;

namespace Tests.Application;

[TestFixture]
public class TaskSubmissionReviewServiceTests
{
    [Test]
    public async Task GetTaskSubmissionReviewAsync_ReturnsAllThreeCategories()
    {
        var service = new MockSubmissionOverviewService();

        var result = await service.GetTaskSubmissionReviewAsync(
            service.DefaultTaskId,
            service.DefaultMemberId);

        Assert.Multiple(() =>
        {
            Assert.That(result.WithoutSubmission.Count, Is.EqualTo(1));
            Assert.That(result.Unreviewed.Count, Is.EqualTo(1));
            Assert.That(result.Reviewed.Count, Is.EqualTo(1));

            Assert.That(
                result.WithoutSubmission[0].Student.FullName,
                Is.EqualTo("Ana Anić"));

            Assert.That(
                result.Unreviewed[0].Student.FullName,
                Is.EqualTo("Marko Marković"));

            Assert.That(
                result.Reviewed[0].Student.FullName,
                Is.EqualTo("Jovana Jovanović"));
        });
    }

    [Test]
    public void GetTaskSubmissionReviewAsync_RejectsUnauthorizedUser()
    {
        var service = new MockSubmissionOverviewService();

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await service.GetTaskSubmissionReviewAsync(
                service.DefaultTaskId,
                Guid.NewGuid()));
    }
}