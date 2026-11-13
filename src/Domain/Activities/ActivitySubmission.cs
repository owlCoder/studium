using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Activities;

/// <summary>
/// Predaja studenta za jednu aktivnost. Važeća je samo dok je u stanju <see cref="ActivitySubmissionState.Submitted"/>.
/// </summary>
public sealed class ActivitySubmission
{
    public Guid Id { get; }
    public Guid ActivityId { get; }
    public Guid StudentId { get; }
    public ActivitySubmissionState State { get; }

    public bool IsValid => State == ActivitySubmissionState.Submitted;

    public ActivitySubmission(Guid id, Guid activityId, Guid studentId,
                              ActivitySubmissionState state = ActivitySubmissionState.Submitted)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Identifikator predaje mora biti zadat.", nameof(id));
        }
        if (activityId == Guid.Empty)
        {
            throw new ArgumentException("Aktivnost predaje mora biti zadata.", nameof(activityId));
        }
        if (studentId == Guid.Empty)
        {
            throw new ArgumentException("Student mora biti zadat.", nameof(studentId));
        }

        Id = id;
        ActivityId = activityId;
        StudentId = studentId;
        State = state;
    }
}
