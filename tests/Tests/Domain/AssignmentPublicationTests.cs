using Domain.Assignments;
using NUnit.Framework;

namespace Tests.Domain;

[TestFixture]
public sealed class AssignmentPublicationTests
{
    [Test]
    public void GetMissingPublicationFields_ReturnsEmptyListWhenAllFieldsArePresent()
    {
        var missing = AssignmentPublication.GetMissingPublicationFields(
            "Zadatak", "Opis", "Uputstvo", DateTimeOffset.UnixEpoch);

        Assert.That(missing, Is.Empty);
    }

    [Test]
    public void GetMissingPublicationFields_ListsEveryMissingField()
    {
        var missing = AssignmentPublication.GetMissingPublicationFields(null, "", "   ", null);

        Assert.That(missing, Is.EqualTo(new[] { "naziv", "opis", "uputstvo", "rok" }));
    }

    [Test]
    public void GetMissingPublicationFields_ListsOnlyMissingFields()
    {
        var missing = AssignmentPublication.GetMissingPublicationFields("Zadatak", "Opis", "  ", null);

        Assert.That(missing, Is.EqualTo(new[] { "uputstvo", "rok" }));
    }
}
