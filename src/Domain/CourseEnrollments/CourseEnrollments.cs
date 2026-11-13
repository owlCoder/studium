using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.CourseEnrollments;

public enum EnrollmentStatus
{
    Active,
    Terminated
}

public sealed class CourseEnrollment
{
    public Guid Id { get; }
    public Guid CourseExecutionId { get; }
    public Guid StudentId { get; }
    public DateTime EnrolledAt { get; }
    public EnrollmentStatus Status { get; private set; }

    public CourseEnrollment(Guid id, Guid courseExecutionId, Guid studentId, DateTime enrolledAt)
    {
        if (id == Guid.Empty) {  
            throw new ArgumentException("Identifikator upisa mora biti zadat.", nameof(id));
        }
        if (courseExecutionId == Guid.Empty)
        {
            throw new ArgumentException("Identifikator izvođenja je obavezan.", nameof(courseExecutionId));
        }
        if (studentId == Guid.Empty) { 
            throw new ArgumentException("Identifikator studenta je obavezan.", nameof(studentId));
        }

        Id = id;
        CourseExecutionId = courseExecutionId;
        StudentId = studentId;
        EnrolledAt = enrolledAt;
        Status = EnrollmentStatus.Active;
    }

    public void Terminate()
    {
        if (Status == EnrollmentStatus.Terminated) { 
            throw new InvalidOperationException("Članstvo je već prekinuto.");
        }

        Status = EnrollmentStatus.Terminated;
    }

    public bool CanSubmitWork() => Status == EnrollmentStatus.Active;
}
