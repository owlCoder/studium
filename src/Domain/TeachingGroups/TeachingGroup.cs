namespace Domain.TeachingGroups;

public enum GroupType
{
    Predavanja,
    AuditorneVezbe,
    LaboratorijskeVezbe
}

public class TeachingGroup
{
    public Guid Id { get; private set; }
    public Guid CourseExecutionId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public GroupType Type { get; private set; }
    public int? Capacity { get; private set; }

    public TeachingGroup(Guid id, Guid courseExecutionId, string code, string name, GroupType type, int? capacity = null)
    {
        if (courseExecutionId == Guid.Empty)
            throw new ArgumentException("Id izvođenja predmeta je obavezan.", nameof(courseExecutionId));

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Oznaka grupe ne sme biti prazna.", nameof(code));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Naziv grupe ne sme biti prazan.", nameof(name));

        if (capacity.HasValue && capacity.Value <= 0)
            throw new ArgumentException("Kapacitet grupe mora biti veći od nule.", nameof(capacity));

        Id = id;
        CourseExecutionId = courseExecutionId;
        Code = code.Trim();
        Name = name.Trim();
        Type = type;
        Capacity = capacity;
    }
}