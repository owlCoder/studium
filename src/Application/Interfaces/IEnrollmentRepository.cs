using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs;
using Domain.Entities;

namespace Application.Interfaces;

public interface IEnrollmentRepository
{
    Task<List<StudentEnrollmentDto>> GetEnrollmentsByOfferingAsync(Guid offeringId);
    Task<Enrollment> GetByIdAsync(Guid enrollmentId);
    Task SaveChangesAsync();
}
