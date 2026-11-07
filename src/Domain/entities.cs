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
        if(EndDate != null)
        {
            throw new InvalidOperationException("Clanstvo je vec zavrseno!");
        }

        if(endDate < StartDate)
        {
            throw new ArgumentException("Datum zavrsetka clanstva ne moze biti pre datuma pocetka!");
        }
        EndDate = endDate;
    }
}

public enum GroupType
{
    Predavanja,
    Vezbe
}

public class Group //8
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public GroupType Type { get; set; } 

    public Guid CourseId { get; set; }
}
