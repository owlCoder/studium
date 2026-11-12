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


[Test]
public void Classify_TenStudentsSevenSubmissionsFourReviewed_ReturnsCorrectCounts()
{
    var taskId = Guid.NewGuid();

    // Deset studenata upisanih na izvođenje.
    var students = Enumerable.Range(1, 10)
        .Select(i => new global::Domain.MemberContract(
            Guid.NewGuid(),
            $"Student {i}",
            "Student"))
        .ToList();

    // Sedam studenata ima predaju:
    // tri nepregledane i četiri pregledane.
    var submissions = students.Take(7)
        .Select((student, index) => new global::Domain.SubmissionContract(
            Guid.NewGuid(),
            taskId,
            student.MemberId,
            $"https://example.com/rad-{index + 1}",
            DateTime.UtcNow.AddMinutes(index),
            index < 3
                ? global::Domain.SubmissionStatus.Pending
                : global::Domain.SubmissionStatus.Accepted))
        .ToList();

    var result = TaskSubmissionReviewClassifier.Classify(
        taskId,
        students,
        submissions);

    Assert.Multiple(() =>
    {
        Assert.That(result.WithoutSubmission.Count, Is.EqualTo(3));
        Assert.That(result.Unreviewed.Count, Is.EqualTo(3));
        Assert.That(result.Reviewed.Count, Is.EqualTo(4));

        Assert.That(
            result.WithoutSubmission.Count +
            result.Unreviewed.Count +
            result.Reviewed.Count,
            Is.EqualTo(10));
    });
}

}