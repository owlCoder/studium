using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Enums;
using Domain.Models;
using NUnit.Framework;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Tests.Domain;

[TestFixture]
public sealed class ContributionTests
{
    private const int AuthorId = 10;
    private const int OtherMemberId = 20;

    private static Contribution MakeSubmitted()
    {
        return new Contribution(7, AuthorId, 5, "Prvi opis", new List<Evidence>());
    }

    private static Contribution MakeReturned()
    {
        var contribution = MakeSubmitted();
        contribution.ReturnForSupplement(99, "Dodaj dokaz", out _);
        return contribution;
    }

    [Test]
    public void Constructor_SetsStatusPodnetAndOneRevision()
    {
        var contribution = MakeSubmitted();

        Assert.Multiple(() =>
        {
            Assert.That(contribution.Status, Is.EqualTo(ContributionStatus.Podnet));
            Assert.That(contribution.Revisions, Has.Count.EqualTo(1));
            Assert.That(contribution.CurrentRevision.Description, Is.EqualTo("Prvi opis"));
        });
    }

    [Test]
    public void Constructor_StoresTastAndPhase()
    {
        var contribution = MakeSubmitted();

        Assert.Multiple(() =>
        {
            Assert.That(contribution.PhaseId, Is.EqualTo(5));
            Assert.That(contribution.IdTask, Is.EqualTo(7));
        });
    }

    [Test]
    public void ReturnForSupplement_WhenSubmitted_ChangesStatusAndSavesNote()
    {
        var contribution = MakeSubmitted();

        var result = contribution.ReturnForSupplement(99, "Dodaj dokaz", out var error);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(contribution.Status, Is.EqualTo(ContributionStatus.VracenNaDopunu));
            Assert.That(contribution.CurrentRevision.ReviewerNote, Is.EqualTo("Dodaj dokaz"));
            Assert.That(error, Is.EqualTo(string.Empty));
        });
    }

    [Test]
    public void ReturnForSupplement_WithEmptyNote_FailsAndKeepsStatus()
    {
        var contribution = MakeSubmitted();

        var result = contribution.ReturnForSupplement(99, "", out var error);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(contribution.Status, Is.EqualTo(ContributionStatus.Podnet));
            Assert.That(error, Is.Not.Empty);
        });
    }

    [Test]
    public void ReturnForSupplement_WhenAlreadyReturned_Fails()
    {
        var contribution = MakeReturned();

        var result = contribution.ReturnForSupplement(99, "Opet", out var error);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(contribution.Status, Is.EqualTo(ContributionStatus.VracenNaDopunu));
            Assert.That(error, Is.Not.Empty);
        });
    }

    [Test]
    public void Supplement_ByAuthorWhenReturned_AddsRevisionAndSetsStatusPodnet()
    {
        var contribution = MakeReturned();

        var result = contribution.Supplement(AuthorId, "Dopunjenm opis", new List<Evidence>(), out var error);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(contribution.Revisions, Has.Count.EqualTo(2));
            Assert.That(contribution.Status, Is.EqualTo(ContributionStatus.Podnet));
            Assert.That(contribution.CurrentRevision.Description, Is.EqualTo("Dopunjen opis"));
            Assert.That(error, Is.EqualTo(string.Empty));
        });
    }

    [Test]

    public void Supplement_KeepsOldRevisionTextAndNote()
    {
        var contribution = MakeReturned();

        contribution.Supplement(AuthorId, "Dopunjen opis", new List<Evidence>(), out _);

        Assert.Multiple(() =>
        {
            Assert.That(contribution.Revisions[0].Description, Is.EqualTo("Prvi opis"));
            Assert.That(contribution.Revisions[0].ReviewerNote, Is.EqualTo("Dodaj dokaz"));
        });
    }

    [Test]
    public void Supplement_ByAnotherMember_FailsAndChangesNothing()
    {
        var contribution = MakeReturned();

        var result = contribution.Supplement(OtherMemberId, "Tudji opis", new List<Evidence>(), out var error);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(contribution.Revisions, Has.Count.EqualTo(1));
            Assert.That(contribution.Status, Is.EqualTo(ContributionStatus.VracenNaDopunu));
            Assert.That(error, Is.Not.Empty);
        });
    }

    [Test]
    public void Supplement_WhenNotReturned_Fails()
    {
        var contribution = MakeSubmitted();

        var result = contribution.Supplement(AuthorId, "Opis", new List<Evidence>(), out var error);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(contribution.Revisions, Has.Count.EqualTo(1));
            Assert.That(contribution.Status, Is.EqualTo(ContributionStatus.Podnet));
            Assert.That(error, Is.Not.Empty);
        });
    }

    [Test]
    public void Supplement_WithEmptyDescription_FailsAndAddsNoRevision()
    {
        var contribution = MakeReturned();

        var result = contribution.Supplement(AuthorId, "", new List<Evidence>(), out var error);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(contribution.Revisions, Has.Count.EqualTo(1));
            Assert.That(contribution.Status, Is.EqualTo(ContributionStatus.VracenNaDopunu));
            Assert.That(error, Is.Not.Empty);
        });
    }

    [Test]
    public void ReturnAndSupplement_CanRepeat_AndKeepsAllRevisions()
    {
        var contribution = MakeSubmitted();

        contribution.ReturnForSupplement(99, "Prva napomena", out _);
        contribution.Supplement(AuthorId, "Drugi opis", new List<Evidence>(), out _);
        contribution.ReturnForSupplement(99, "Druga napomena", out _);
        contribution.Supplement(AuthorId, "Treci opis", new List<Evidence>(), out _);

        Assert.Multiple(() =>
        {
            Assert.That(contribution.Revisions, Has.Count.EqualTo(3));
            Assert.That(contribution.Status, Is.EqualTo(ContributionStatus.Podnet));
            Assert.That(contribution.CurrentRevision.Description, Is.EqualTo("Treci opis"));
        });
    }
}
