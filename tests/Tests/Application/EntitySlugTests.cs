using Application.Common;
using NUnit.Framework;

namespace Tests.Application;

[TestFixture]
public sealed class EntitySlugTests
{
    private static readonly Guid Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid AbcdeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaabcde");

    [Test]
    public void Create_CombinesPrefixAndLastFiveCharactersOfId()
    {
        Assert.Multiple(() =>
        {
            Assert.That(EntitySlug.Create("ERS", Id), Is.EqualTo("ERS-11111"));
            Assert.That(EntitySlug.Create("  ERS  ", Id), Is.EqualTo("ERS-11111"));
            Assert.That(EntitySlug.Create("ERS", AbcdeId), Is.EqualTo("ERS-abcde"));
        });
    }

    [Test]
    public void Create_ReplacesWhitespaceAndUnsafeCharactersWithSingleDash()
    {
        Assert.Multiple(() =>
        {
            Assert.That(EntitySlug.Create("Timski projekat", AbcdeId), Is.EqualTo("Timski-projekat-abcde"));
            Assert.That(EntitySlug.Create("Uvod/u temu", AbcdeId), Is.EqualTo("Uvod-u-temu-abcde"));
            Assert.That(EntitySlug.Create("A   B", AbcdeId), Is.EqualTo("A-B-abcde"));
        });
    }

    [Test]
    public void Create_WithoutPrefix_ReturnsOnlyIdSuffix()
    {
        Assert.That(EntitySlug.Create("   ", Id), Is.EqualTo("11111"));
    }

    [Test]
    public void IdSuffix_ReturnsLastFiveCharactersWithoutHyphens()
    {
        Assert.That(EntitySlug.IdSuffix(Id), Is.EqualTo("11111"));
        Assert.That(EntitySlug.IdSuffix(Id), Has.Length.EqualTo(EntitySlug.IdSuffixLength));
    }

    [Test]
    public void Matches_IgnoresCaseAndSurroundingWhitespace()
    {
        Assert.Multiple(() =>
        {
            Assert.That(EntitySlug.Matches("ERS-11111", "ERS", Id), Is.True);
            Assert.That(EntitySlug.Matches("ers-11111", "ERS", Id), Is.True);
            Assert.That(EntitySlug.Matches(" ERS-11111 ", "ERS", Id), Is.True);
            Assert.That(EntitySlug.Matches("ERS-22222", "ERS", Id), Is.False);
            Assert.That(EntitySlug.Matches(Id.ToString(), "ERS", Id), Is.False);
            Assert.That(EntitySlug.Matches(null, "ERS", Id), Is.False);
        });
    }
}
