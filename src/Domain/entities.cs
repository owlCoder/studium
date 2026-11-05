namespace Domain.Entities;

public class GroupMembership
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public Guid StudentId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; } 

    public void Remove(DateTime endDate)
    {
        EndDate = endDate;
    }
}