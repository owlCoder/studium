using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Application.Services;
using Domain.Entities;
using Infrastructure.Repositories;

namespace Tests;

[TestFixture]
public class EnrollmentServiceTests
{
    [Test]
    public async Task SearchAsync_ShouldReturnOnlyActiveEnrollments_ForGivenOffering()
    {
        var offeringId = Guid.NewGuid();

        var active = new Enrollment { OfferingId = offeringId, StudentId = Guid.NewGuid() };
        var otherOffering = new Enrollment { OfferingId = Guid.NewGuid(), StudentId = Guid.NewGuid() };
        var terminated = new Enrollment { OfferingId = offeringId, StudentId = Guid.NewGuid() };
        terminated.Terminate();

        var service = new EnrollmentService(
            new EnrollmentRepository(new List<Enrollment> { active, otherOffering, terminated }));

        var result = await service.SearchAsync(offeringId);

        Assert.That(result, Has.Count.EqualTo(1), "Treba vratiti samo aktivne studente sa tog izvođenja.");
        Assert.That(result[0].EnrollmentId, Is.EqualTo(active.Id));
        Assert.That(result[0].StudentId, Is.EqualTo(active.StudentId), "Podaci o studentu moraju biti tačni.");
        Assert.That(result[0].IsActive, Is.True);
    }

    [Test]
    public async Task SearchAsync_ShouldReturnEmptyList_WhenNoEnrollments()
    {
        var service = new EnrollmentService(new EnrollmentRepository());

        var result = await service.SearchAsync(Guid.NewGuid());

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task TerminateAsync_ShouldReturnTrue_WhenOfferingAndEnrollmentMatch()
    {
        var offeringId = Guid.NewGuid();
        var enrollment = new Enrollment { OfferingId = offeringId, StudentId = Guid.NewGuid() };

        var repository = new EnrollmentRepository(new List<Enrollment> { enrollment });
        var service = new EnrollmentService(repository);

        var result = await service.TerminateAsync(offeringId, enrollment.Id, Guid.NewGuid());

        Assert.That(result, Is.True, "Metoda treba da vrati true kada su podaci ispravni.");
        Assert.That(enrollment.IsActive, Is.False, "Status članstva mora biti neaktivan.");
        Assert.That(await repository.GetByIdAsync(enrollment.Id), Is.Not.Null,
            "Članstvo ne sme biti fizički obrisano.");
    }

    [Test]
    public async Task TerminateAsync_ShouldReturnFalse_WhenOfferingIdDoesNotMatch()
    {
        var enrollment = new Enrollment { OfferingId = Guid.NewGuid(), StudentId = Guid.NewGuid() };

        var service = new EnrollmentService(
            new EnrollmentRepository(new List<Enrollment> { enrollment }));

        var result = await service.TerminateAsync(Guid.NewGuid(), enrollment.Id, Guid.NewGuid());

        Assert.That(result, Is.False, "Metoda mora vratiti false ako članstvo ne pripada izvođenju.");
        Assert.That(enrollment.IsActive, Is.True, "Status ne sme da se promeni.");
    }

    [Test]
    public async Task TerminateAsync_ShouldReturnFalse_WhenEnrollmentDoesNotExist()
    {
        var service = new EnrollmentService(new EnrollmentRepository());

        var result = await service.TerminateAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        Assert.That(result, Is.False);
    }
}
