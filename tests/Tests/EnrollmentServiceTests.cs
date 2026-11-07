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
    public async Task TerminateAsync_ShouldReturnTrue_WhenOfferingAndEnrollmentMatch()
    {
        var offeringId = Guid.NewGuid();
        var enrollmentId = Guid.NewGuid();

        var enrollment = new Enrollment
        {
            Id = enrollmentId,
            OfferingId = offeringId,
            IsActive = true
        };

        var repository = new EnrollmentRepository(new List<Enrollment> { enrollment });
        var service = new EnrollmentService(repository);

        var result = await service.TerminateAsync(offeringId, enrollmentId, Guid.NewGuid());

        Assert.IsTrue(result, "Metoda treba da vrati true kada su podaci ispravni.");
        Assert.IsFalse(enrollment.IsActive, "Status članstva mora biti postavljen na false (neaktivan).");
    }

    [Test]
    public async Task TerminateAsync_ShouldReturnFalse_WhenOfferingIdDoesNotMatch()
    {
        var correctOfferingId = Guid.NewGuid();
        var wrongOfferingId = Guid.NewGuid();
        var enrollmentId = Guid.NewGuid();

        var enrollment = new Enrollment
        {
            Id = enrollmentId,
            OfferingId = correctOfferingId,
            IsActive = true
        };

        var repository = new EnrollmentRepository(new List<Enrollment> { enrollment });
        var service = new EnrollmentService(repository);

        var result = await service.TerminateAsync(wrongOfferingId, enrollmentId, Guid.NewGuid());

        Assert.IsFalse(result, "Metoda mora da vrati false ako offeringId ne poklapa sa članstvom.");
        Assert.IsTrue(enrollment.IsActive, "Status ne sme da se promeni ukoliko je pokušaj neovlašćen.");
    }
}
