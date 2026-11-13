namespace Domain.Enrollments;

public sealed class Membership
{
    public Guid Id { get; }
    public Guid StudentId { get; }    
    public Guid OfferingId { get; }   
    public MembershipStatus Status { get; private set; } 

    public bool IsActive => Status == MembershipStatus.Active; 

    public Membership(Guid id, Guid studentId, Guid offeringId,
                      MembershipStatus status = MembershipStatus.Active)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Identifikator članstva mora biti zadat.", nameof(id));
        }
        if (studentId == Guid.Empty)
        {
            throw new ArgumentException("Student mora biti zadat.", nameof(studentId));
        }
        if (offeringId == Guid.Empty)
        {
            throw new ArgumentException("Izvođenje mora biti zadato.", nameof(offeringId));
        }

        Id = id;
        StudentId = studentId;
        OfferingId = offeringId;
        Status = status;
    }

    public void Terminate()   
    {
        Status = MembershipStatus.Terminated;
    }
}
