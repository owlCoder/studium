using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Services;

public class EnrollmentService
{
    private readonly IEnrollmentRepository _repository;

    public EnrollmentService(IEnrollmentRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<StudentEnrollmentDto>> SearchAsync(Guid offeringId)
    {
        return await _repository.GetEnrollmentsByOfferingAsync(offeringId);
    }

    public async Task<bool> TerminateAsync(Guid offeringId, Guid enrollmentId, Guid teacherId)
    {
        var enrollment = await _repository.GetByIdAsync(enrollmentId);

        if (enrollment == null || enrollment.OfferingId != offeringId)
        {
            return false;
        }

        if (teacherId == Guid.Empty)
        {
            return false;
        }

        enrollment.Terminate();
        await _repository.SaveChangesAsync();

        return true;
    }
}
