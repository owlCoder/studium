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
    private Guid _offeringId;
    private Guid _teacherId;
    private InMemoryOfferingAccessReader _access = null!;

    [SetUp]
    public void SetUp()
    {
        _offeringId = Guid.NewGuid();
        _teacherId = Guid.NewGuid();
        _access = new InMemoryOfferingAccessReader(new[] { (_teacherId, _offeringId) });
    }

    // ---------- ST: pretraga upisanih studenata ----------

    [Test]
    public async Task SearchAsync_ShouldReturnOnlyActiveEnrollments_ForGivenOffering()
    {
        var active = new Enrollment { OfferingId = _offeringId, StudentId = Guid.NewGuid() };
        var otherOffering = new Enrollment { OfferingId = Guid.NewGuid(), StudentId = Guid.NewGuid() };
        var terminated = new Enrollment { OfferingId = _offeringId, StudentId = Guid.NewGuid() };
        terminated.Terminate();

        var service = new EnrollmentService(
            new EnrollmentRepository(new List<Enrollment> { active, otherOffering, terminated }), _access);

        var result = await service.SearchAsync(_offeringId, _teacherId);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(1), "Treba vratiti samo aktivne studente sa tog izvođenja.");
        Assert.That(result![0].EnrollmentId, Is.EqualTo(active.Id));
        Assert.That(result[0].StudentId, Is.EqualTo(active.StudentId), "Podaci o studentu moraju biti tačni.");
        Assert.That(result[0].IsActive, Is.True);
    }

    [Test]
    public async Task SearchAsync_ShouldReturnEmptyList_WhenNoEnrollments()
    {
        var service = new EnrollmentService(new EnrollmentRepository(), _access);

        var result = await service.SearchAsync(_offeringId, _teacherId);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task SearchAsync_ShouldReturnNull_WhenTeacherHasNoAccess()
    {
        var enrollment = new Enrollment { OfferingId = _offeringId, StudentId = Guid.NewGuid() };
        var service = new EnrollmentService(
            new EnrollmentRepository(new List<Enrollment> { enrollment }), _access);

        var result = await service.SearchAsync(_offeringId, Guid.NewGuid());

        Assert.That(result, Is.Null, "Nastavnik bez prava ne sme videti spisak.");
    }

    // ---------- ST: prekid članstva ----------

    [Test]
    public async Task TerminateAsync_ShouldSucceed_WhenTeacherHasAccess()
    {
        var enrollment = new Enrollment { OfferingId = _offeringId, StudentId = Guid.NewGuid() };
        var repository = new EnrollmentRepository(new List<Enrollment> { enrollment });
        var service = new EnrollmentService(repository, _access);

        var result = await service.TerminateAsync(_offeringId, enrollment.Id, _teacherId);

        Assert.That(result, Is.EqualTo(EnrollmentOperationResult.Success));
        Assert.That(enrollment.IsActive, Is.False, "Status članstva mora biti neaktivan.");
        Assert.That(await repository.GetByIdAsync(enrollment.Id), Is.Not.Null,
            "Članstvo ne sme biti fizički obrisano.");
    }

    [Test]
    public async Task TerminateAsync_ShouldReturnForbidden_WhenTeacherHasNoAccess()
    {
        var enrollment = new Enrollment { OfferingId = _offeringId, StudentId = Guid.NewGuid() };
        var service = new EnrollmentService(
            new EnrollmentRepository(new List<Enrollment> { enrollment }), _access);

        var result = await service.TerminateAsync(_offeringId, enrollment.Id, Guid.NewGuid());

        Assert.That(result, Is.EqualTo(EnrollmentOperationResult.Forbidden));
        Assert.That(enrollment.IsActive, Is.True, "Status ne sme da se promeni.");
    }

    [Test]
    public async Task TerminateAsync_ShouldReturnNotFound_WhenEnrollmentBelongsToOtherOffering()
    {
        var enrollment = new Enrollment { OfferingId = Guid.NewGuid(), StudentId = Guid.NewGuid() };
        var service = new EnrollmentService(
            new EnrollmentRepository(new List<Enrollment> { enrollment }), _access);

        var result = await service.TerminateAsync(_offeringId, enrollment.Id, _teacherId);

        Assert.That(result, Is.EqualTo(EnrollmentOperationResult.NotFound));
        Assert.That(enrollment.IsActive, Is.True);
    }

    [Test]
    public async Task TerminateAsync_ShouldReturnNotFound_WhenEnrollmentDoesNotExist()
    {
        var service = new EnrollmentService(new EnrollmentRepository(), _access);

        var result = await service.TerminateAsync(_offeringId, Guid.NewGuid(), _teacherId);

        Assert.That(result, Is.EqualTo(EnrollmentOperationResult.NotFound));
    }
}
