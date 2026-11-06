namespace Studium.Domain.Entities;

public enum TeamCompositionStatus
{
    Open,
    Locked
}

public class StudentTeam
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CourseOfferingId { get; set; }

    public Guid ProjectActivityId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int MinSize { get; set; }

    public int MaxSize { get; set; }

    public List<Guid> MemberStudentIds { get; set; } = new();

    public List<Guid> PendingInvitationStudentIds { get; set; } = new();

    public TeamCompositionStatus Status { get; set; } = TeamCompositionStatus.Open;

    public static StudentTeam Create(
        Guid courseOfferingId,
        Guid projectActivityId,
        string name,
        Guid creatorStudentId,
        int minSize,
        int maxSize)
    {
        if (courseOfferingId == Guid.Empty)
        {
            throw new ArgumentException("A course offering must be specified.", nameof(courseOfferingId));
        }

        if (projectActivityId == Guid.Empty)
        {
            throw new ArgumentException("A project activity must be specified.", nameof(projectActivityId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A team name cannot be empty.", nameof(name));
        }

        if (creatorStudentId == Guid.Empty)
        {
            throw new ArgumentException("A creator student must be specified.", nameof(creatorStudentId));
        }

        if (minSize < 1)
        {
            throw new ArgumentException("Minimum team size must be at least 1.", nameof(minSize));
        }

        if (maxSize < minSize)
        {
            throw new ArgumentException("Maximum team size cannot be less than minimum size.", nameof(maxSize));
        }

        return new StudentTeam
        {
            CourseOfferingId = courseOfferingId,
            ProjectActivityId = projectActivityId,
            Name = name.Trim(),
            MinSize = minSize,
            MaxSize = maxSize,
            MemberStudentIds = new List<Guid> { creatorStudentId },
            PendingInvitationStudentIds = new List<Guid>(),
            Status = TeamCompositionStatus.Open
        };
    }
}
