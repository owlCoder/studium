using System;

namespace Application.DTOs;

public record StudentEnrollmentDto(Guid EnrollmentId, Guid StudentId, bool IsActive);
