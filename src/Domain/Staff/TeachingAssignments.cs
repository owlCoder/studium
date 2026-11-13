namespace Domain.Staff;


/// Angažovanje saradnika na izvođenju (ST-005).

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
}
