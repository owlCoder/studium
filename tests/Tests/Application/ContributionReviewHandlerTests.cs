using Application.Contributions;
using Domain.Enums;
using Domain.Models;
using Infrastructure.Contributions;
using NUnit.Framework;

namespace Tests.Application;

[TestFixture]
public class ContributionReviewHandlerTests
{
    private const int StudentId = 10;
    private const int TeacherId = 99;

    private InMemoryContributionReader _store = null!;
    private ContributionReviewHandler _handler = null!;

    private static readonly Viewer Teacher = new(TeacherId, ViewerRole.Teacher);
    private static readonly Viewer Student = new(StudentId, ViewerRole.Student);

    [SetUp]
    public void SetUp()
    {
        _store = new InMemoryContributionReader();
        _store.Add(new Contribution(1, 7, StudentId, 5, "Opis", Array.Empty<Evidence>()));
        _handler = new ContributionReviewHandler(_store);
    }

    [Test]
    public void Confirm_ByTeacher_Succeeds()
    {
        var result = _handler.Confirm(Teacher, 1, "Odobreno");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(_store.GetById(1)!.Status, Is.EqualTo(ContributionStatus.Potvrdjen));
        });
    }

    [Test]
    public void Confirm_ByStudent_IsRejected()
    {
        var result = _handler.Confirm(Student, 1, "Odobreno");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(_store.GetById(1)!.Status, Is.EqualTo(ContributionStatus.Podnet));
        });
    }

    [Test]
    public void Confirm_NonExistingContribution_Fails()
    {
        var result = _handler.Confirm(Teacher, 404, "Odobreno");

        Assert.That(result.Success, Is.False);
    }

    [Test]
    public void Confirm_WithEmptyNote_FailsAndKeepsStatus()
    {
        var result = _handler.Confirm(Teacher, 1, "");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(_store.GetById(1)!.Status, Is.EqualTo(ContributionStatus.Podnet));
        });
    }

    [Test]
    public void ReturnForSupplement_ByTeacher_Succeeds()
    {
        var result = _handler.ReturnForSupplement(Teacher, 1, "Dodaj dokaz");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(_store.GetById(1)!.Status, Is.EqualTo(ContributionStatus.VracenNaDopunu));
        });
    }

    [Test]
    public void ReturnForSupplement_ByStudent_IsRejected()
    {
        var result = _handler.ReturnForSupplement(Student, 1, "Dodaj dokaz");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(_store.GetById(1)!.Status, Is.EqualTo(ContributionStatus.Podnet));
        });
    }

    [Test]
    public void ReturnForSupplement_WithEmptyNote_Fails()
    {
        var result = _handler.ReturnForSupplement(Teacher, 1, " ");

        Assert.That(result.Success, Is.False);
    }
}
