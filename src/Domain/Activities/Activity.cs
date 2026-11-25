using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Activities;

/// <summary>
/// Aktivnost (obaveza) studenta na jednom izvođenju predmeta, onako kako je vidi pravilo otvorene aktivnosti.
/// Nije kopija zadatka iz ST-011: sadrži samo polja koja pravilo zahteva.
/// ActivityType je oznaka vrste iz Mileninog registra (US-7.6), npr. "assignment".
/// </summary>
public sealed class Activity
{
    public Guid Id { get; }
    public Guid OfferingId { get; }
    public string ActivityType { get; }
    public bool IsPublished { get; }
    public DateTimeOffset Deadline { get; }

    public Activity(Guid id, Guid offeringId, string activityType, bool isPublished, DateTimeOffset deadline)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Identifikator aktivnosti mora biti zadat.", nameof(id));
        }
        if (offeringId == Guid.Empty)
        {
            throw new ArgumentException("Izvođenje aktivnosti mora biti zadato.", nameof(offeringId));
        }
        if (string.IsNullOrWhiteSpace(activityType))
        {
            throw new ArgumentException("Vrsta aktivnosti je obavezna.", nameof(activityType));
        }

        Id = id;
        OfferingId = offeringId;
        ActivityType = activityType.Trim().ToLowerInvariant();
        IsPublished = isPublished;
        Deadline = deadline;
    }
}
