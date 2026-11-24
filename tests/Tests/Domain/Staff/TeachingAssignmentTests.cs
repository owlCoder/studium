using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Staff;
using NUnit.Framework;

namespace Tests.Domain.Staff;

[TestFixture]
internal class TeachingAssignmentTests
{
    private static TeachingAssignment NewAssignment() =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Responsibility.Assistant);

    [Test]
    public void NovoAngazovanje_JeAktivno()
    {
        var assignment = NewAssignment();

        Assert.Multiple(() =>
        {
            Assert.That(assignment.IsActive, Is.True);
            Assert.That(assignment.RemovedAt, Is.Null);
        });
    }

    [Test]
    public void Remove_ZapisujeKoJeIKadaUklonio_IAngazovanjeViseNijeAktivno()
    {
        var assignment = NewAssignment();
        var removedBy = Guid.NewGuid();
        var removedAt = new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

        assignment.Remove(removedBy, removedAt);

        Assert.Multiple(() =>
        {
            Assert.That(assignment.IsActive, Is.False);
            Assert.That(assignment.RemovedAt, Is.EqualTo(removedAt));
            Assert.That(assignment.RemovedBy, Is.EqualTo(removedBy));
        });
    }

    [Test]
    public void Remove_DvaPuta_JeOdbijeno()
    {
        var assignment = NewAssignment();
        assignment.Remove(Guid.NewGuid(), DateTime.UtcNow);

        Assert.That(
            () => assignment.Remove(Guid.NewGuid(), DateTime.UtcNow),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void Remove_BezIdentitetaKoUklanja_JeOdbijeno()
    {
        var assignment = NewAssignment();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => assignment.Remove(Guid.Empty, DateTime.UtcNow),
                Throws.TypeOf<ArgumentException>());
            Assert.That(assignment.IsActive, Is.True);
        });
    }

    [Test]
    public void Constructor_OdbijaPraznogKorisnika()
    {
        Assert.That(
            () => new TeachingAssignment(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, Responsibility.Teacher),
            Throws.TypeOf<ArgumentException>());
    }
}
