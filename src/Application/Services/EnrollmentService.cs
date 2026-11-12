using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Interfaces;

namespace Application.Services;

public enum EnrollmentOperationResult
{
    Success,
    NotFound,
    Forbidden
}

public class EnrollmentService
{
    private readonly IEnrollmentRepository _repository;
    private readonly IOfferingAccessReader _access;

    public EnrollmentService(IEnrollmentRepository repository, IOfferingAccessReader access)
    {
        _repository = repository;
        _access = access;
    }

    /// <returns>null ako nastavnik nema pravo nad izvođenjem.</returns>
    public async Task<List<StudentEnrollmentDto>?> SearchAsync(Guid offeringId, Guid teacherId)
    {
        if (!await _access.IsTeacherOfOfferingAsync(teacherId, offeringId))
        {
            return null;
        }

        return await _repository.GetEnrollmentsByOfferingAsync(offeringId);
    }

    public async Task<EnrollmentOperationResult> TerminateAsync(Guid offeringId, Guid enrollmentId, Guid teacherId)
    {
        if (!await _access.IsTeacherOfOfferingAsync(teacherId, offeringId))
        {
            return EnrollmentOperationResult.Forbidden;
        }

        var enrollment = await _repository.GetByIdAsync(enrollmentId);
        if (enrollment == null || enrollment.OfferingId != offeringId)
        {
            return EnrollmentOperationResult.NotFound;
        }

        enrollment.Terminate();
        await _repository.SaveChangesAsync();

        return EnrollmentOperationResult.Success;
    }
}
