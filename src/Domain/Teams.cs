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
            Console.WriteLine("A course offering must be specified.");
        }

        if (projectActivityId == Guid.Empty)
        {
            Console.WriteLine("A project activity must be specified.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            Console.WriteLine("A team name cannot be empty.");
        }

        if (creatorStudentId == Guid.Empty)
        {
            Console.WriteLine("A creator student must be specified.");
        }

        if (minSize < 1)
        {
            Console.WriteLine("Minimum team size must be at least 1.");
        }

        if (maxSize < minSize)
        {
            Console.WriteLine("Maximum team size cannot be less than minimum size.");
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
