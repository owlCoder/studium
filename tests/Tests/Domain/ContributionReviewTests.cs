using Domain.Enums;
using Domain.Models;
using NUnit.Framework;

namespace Tests.Domain;

[TestFixture]
public sealed class ContributionReviewTests
{
    private const int AuthorId = 10;
    private const int ReviewerId = 99;

    private static Contribution MakeSubmitted()
    {
        return new Contribution(1, 7, AuthorId, 5, "Prvi opis", new List<Evidence>());
    }

    private static Contribution MakeConfirmed()
    {
        var contribution = MakeSubmitted();
        contribution.Confirm(ReviewerId, "Odobreno", out _);
        return contribution;
    }

    private static Contribution MakeReturned()
    {
        var contribution = MakeSubmitted();
        contribution.ReturnForSupplement(ReviewerId, "Dodaj dokaz", out _);
        return contribution;
    }

    // ---------- Confirm ----------

    [Test]
    public void Confirm_WhenSubmitted_SetsStatusConfirmed()
    {
        var contribution = MakeSubmitted();

        var result = contribution.Confirm(ReviewerId, "Odobreno", out var error);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(error, Is.Empty);
            Assert.That(contribution.Status, Is.EqualTo(ContributionStatus.Potvrdjen));
        });
    }

    [Test]
    public void Confirm_SavesReviewerNote()
    {
        var contribution = MakeSubmitted();

        contribution.Confirm(ReviewerId, "Odobreno", out _);

        Assert.That(contribution.CurrentRevision.ReviewerNote, Is.EqualTo("Odobreno"));
    }

    [Test]
    public void Confirm_RecordsReviewerAndTime()
    {
        var contribution = MakeSubmitted();
        var before = DateTime.Now;

        contribution.Confirm(ReviewerId, "Odobreno", out _);

        Assert.Multiple(() =>
        {
            Assert.That(contribution.CurrentRevision.ChangedBy, Is.EqualTo(ReviewerId));
            Assert.That(contribution.CurrentRevision.ChangedAt, Is.Not.Null);
            Assert.That(contribution.CurrentRevision.ChangedAt, Is.GreaterThanOrEqualTo(before));
        });
    }

    [Test]
    public void Confirm_UpdatesContributionChangedAt()
    {
        var contribution = MakeSubmitted();
        var before = DateTime.Now;

        contribution.Confirm(ReviewerId, "Odobreno", out _);

        Assert.That(contribution.ChangedAt, Is.GreaterThanOrEqualTo(before));
    }

    [Test]
    public void Confirm_WithEmptyNote_FailsAndKeepsStatus()
    {
        var contribution = MakeSubmitted();

        var result = contribution.Confirm(ReviewerId, "", out var error);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(error, Is.Not.Empty);
            Assert.That(contribution.Status, Is.EqualTo(ContributionStatus.Podnet));
        });
    }

    [Test]
    public void Confirm_WithEmptyNote_DoesNotRecordReviewer()
    {
        var contribution = MakeSubmitted();

        contribution.Confirm(ReviewerId, "   ", out _);

        Assert.Multiple(() =>
        {
            Assert.That(contribution.CurrentRevision.ChangedBy, Is.Null);
            Assert.That(contribution.CurrentRevision.ChangedAt, Is.Null);
        });
    }

    [Test]
    public void Confirm_WhenAlreadyConfirmed_Fails()
    {
        var contribution = MakeConfirmed();

        var result = contribution.Confirm(ReviewerId, "Opet", out var error);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(error, Is.Not.Empty);
            Assert.That(contribution.Status, Is.EqualTo(ContributionStatus.Potvrdjen));
        });
    }

    [Test]
    public void Confirm_WhenReturnedForSupplement_FailsAndKeepsStatus()
    {
        var contribution = MakeReturned();

        var result = contribution.Confirm(ReviewerId, "Odobreno", out var error);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(error, Is.Not.Empty);
            Assert.That(contribution.Status, Is.EqualTo(ContributionStatus.VracenNaDopunu));
        });
    }

    // ---------- ReturnForSupplement ----------

    [Test]
    public void ReturnForSupplement_RecordsReviewerAndTime()
    {
        var contribution = MakeSubmitted();
        var before = DateTime.Now;

        contribution.ReturnForSupplement(ReviewerId, "Dodaj dokaz", out _);

        Assert.Multiple(() =>
        {
            Assert.That(contribution.CurrentRevision.ChangedBy, Is.EqualTo(ReviewerId));
            Assert.That(contribution.CurrentRevision.ChangedAt, Is.Not.Null);
            Assert.That(contribution.CurrentRevision.ChangedAt, Is.GreaterThanOrEqualTo(before));
        });
    }

    [Test]
    public void ReturnForSupplement_WithEmptyNote_DoesNotRecordReviewer()
    {
        var contribution = MakeSubmitted();

        contribution.ReturnForSupplement(ReviewerId, "", out _);

        Assert.Multiple(() =>
        {
            Assert.That(contribution.CurrentRevision.ChangedBy, Is.Null);
            Assert.That(contribution.CurrentRevision.ChangedAt, Is.Null);
        });
    }

    [Test]
    public void ReturnForSupplement_WhenConfirmed_FailsAndKeepsStatus()
    {
        var contribution = MakeConfirmed();

        var result = contribution.ReturnForSupplement(ReviewerId, "Ipak dopuni", out var error);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(error, Is.Not.Empty);
            Assert.That(contribution.Status, Is.EqualTo(ContributionStatus.Potvrdjen));
        });
    }

    // ---------- Potvrdjen doprinos se ne dopunjuje ----------

    [Test]
    public void Supplement_WhenConfirmed_Fails()
    {
        var contribution = MakeConfirmed();

        var result = contribution.Supplement(AuthorId, "Novi opis", new List<Evidence>(), out var error);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(error, Is.Not.Empty);
            Assert.That(contribution.Revisions, Has.Count.EqualTo(1));
        });
    }
}
