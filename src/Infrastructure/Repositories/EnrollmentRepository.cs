using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;

namespace Infrastructure.Repositories;

public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly List<Enrollment> _enrollments;

    public EnrollmentRepository(IEnumerable<Enrollment>? enrollments = null)
    {
        _enrollments = enrollments?.ToList() ?? new List<Enrollment>();
    }

    public Task<List<StudentEnrollmentDto>> GetEnrollmentsByOfferingAsync(Guid offeringId)
    {
        var enrollments = _enrollments
            .Where(e => e.OfferingId == offeringId)
            .Select(e => new StudentEnrollmentDto(e.Id, e.StudentId, e.IsActive))
            .ToList();

        return Task.FromResult(enrollments);
    }

    public Task<Enrollment> GetByIdAsync(Guid enrollmentId)
    {
        var enrollment = _enrollments
            .FirstOrDefault(e => e.Id == enrollmentId);

        return Task.FromResult(enrollment!);
    }

    public Task SaveChangesAsync()
    {
        return Task.CompletedTask;
    }
}
