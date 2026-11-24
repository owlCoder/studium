using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Staff;

public sealed class TeachingAssignment
{
    public Guid Id { get; }
    public Guid OfferingId { get; }
    public Guid UserId { get; }
    public Responsibility Responsibility { get; private set; }
    public DateTime? RemovedAt { get; private set; }
    public Guid? RemovedBy { get; private set; }

    public bool IsActive => RemovedAt is null;

    public TeachingAssignment(Guid id, Guid offeringId, Guid userId, Responsibility responsibility)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Identifikator mora biti zadat.", nameof(id));
        }

        if (offeringId == Guid.Empty)
        {
            throw new ArgumentException("Izvođenje mora biti zadato.", nameof(offeringId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("Korisnik mora biti zadat.", nameof(userId));
        }

        Id = id;
        OfferingId = offeringId;
        UserId = userId;
        Responsibility = responsibility;
    }

    public void PromoteToResponsible()
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("Uklonjeno angažovanje se ne može menjati.");
        }

        Responsibility = Responsibility.ResponsibleTeacher;
    }

    /// <summary>
    /// US-7.2: saradnik je uklonjen sa izvođenja. Zapis se ne briše, samo prestaje da bude aktivan,
    /// pa ga upit angažovanja više ne vraća.
    /// </summary>

    public void Remove(Guid removedBy, DateTime removedAt)
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("Angažovanje je već uklonjeno.");
        }

        if (removedBy == Guid.Empty)
        {
            throw new ArgumentException("Mora biti poznato ko uklanja saradnika.", nameof(removedBy));
        }

        RemovedAt = removedAt;
        RemovedBy = removedBy;
    }

}
