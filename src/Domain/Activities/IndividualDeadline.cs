using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Activities;

/// <summary>
/// US-7.9: pojedinačni rok jednog studenta za jednu aktivnost (produženje roka iz ST-015).
/// Kada postoji, ima prednost nad zajedničkim rokom aktivnosti, isto kao u ST-015
/// (<c>StudentZadatakRok.IzracunajPrimenljiviRok</c>).
/// </summary>
public sealed class IndividualDeadline
{
    public Guid ActivityId { get; }
    public Guid StudentId { get; }
    public DateTimeOffset Deadline { get; }

    public IndividualDeadline(Guid activityId, Guid studentId, DateTimeOffset deadline)
    {
        if (activityId == Guid.Empty)
        {
            throw new ArgumentException("Aktivnost mora biti zadata.", nameof(activityId));
        }
        if (studentId == Guid.Empty)
        {
            throw new ArgumentException("Student mora biti zadat.", nameof(studentId));
        }

        ActivityId = activityId;
        StudentId = studentId;
        Deadline = deadline;
    }
}
