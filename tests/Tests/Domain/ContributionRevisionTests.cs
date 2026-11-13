using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Models;
using NUnit.Framework;

namespace Tests.Domain;

[TestFixture]
public sealed class ContributionRevisionTests
{
    private static ContributionRevision Make(string? description)
    {
        return new ContributionRevision(1, 1, 10, description!, new List<Evidence>());
    }

    [Test]
    public void IsValid_WithDescription_ReturnsTrue()
    {
        var revision = Make("Uradio sam pretragu");

        var result = revision.IsValid(out var error);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(error, Is.EqualTo(string.Empty));
        });
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void IsValid_WithEmptyDescription_ReturnsFalseWithError(string? description)
    {
        var revision = Make(description);

        var result = revision.IsValid(out var error);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(error, Is.Not.Empty);
        });
    }

    [Test]
    public void SetReviewerNote_WithText_SaveNote()
    {
        var revision = Make("Opis");

        var result = revision.SetReviewerNote("Dodaj link ka commit-u", out var error);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(revision.ReviewerNote, Is.EqualTo("Dodaj link ka commit-u"));
            Assert.That(error, Is.EqualTo(string.Empty));
        });
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void SetReviewerNote_WithEmptyNote_ReturnsFalseAndKeepsNoteEmpty(string? note)
    {
        var revision = Make("OOpis");

        var result = revision.SetReviewerNote(note!, out var error);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(revision.ReviewerNote, Is.EqualTo(string.Empty));
            Assert.That(error, Is.Not.Empty);
        });
    }
}
