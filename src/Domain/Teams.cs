namespace Studium.Domain.Entities;

public enum TeamCompositionStatus
{
    Open,
    Locked
}

/// <summary>
/// Tim studenata vezan za izvođenje predmeta i projektnu aktivnost.
/// Neispravni podaci se ne signaliziraju izuzetkom, već rezultatom
/// <see cref="TeamCreation"/>.
/// </summary>
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

    public static TeamCreation TryCreate(
        Guid courseOfferingId,
        Guid projectActivityId,
        string? name,
        Guid creatorStudentId,
        int minSize,
        int maxSize)
    {
        if (courseOfferingId == Guid.Empty)
        {
            return TeamCreation.Failure("Izvođenje predmeta mora biti zadato");
        }

        if (projectActivityId == Guid.Empty)
        {
            return TeamCreation.Failure("Projektna aktivnost mora biti zadata");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return TeamCreation.Failure("Naziv tima mora biti zadat");
        }

        if (creatorStudentId == Guid.Empty)
        {
            return TeamCreation.Failure("Tvorac tima mora biti zadat");
        }

        if (minSize < 1)
        {
            return TeamCreation.Failure("Najmanji broj članova tima mora biti najmanje 1");
        }

        if (maxSize < minSize)
        {
            return TeamCreation.Failure("Najveći broj članova tima ne sme biti manji od najmanjeg");
        }

        return TeamCreation.Success(new StudentTeam
        {
            CourseOfferingId = courseOfferingId,
            ProjectActivityId = projectActivityId,
            Name = name.Trim(),
            MinSize = minSize,
            MaxSize = maxSize,
            MemberStudentIds = new List<Guid> { creatorStudentId },
            PendingInvitationStudentIds = new List<Guid>(),
            Status = TeamCompositionStatus.Open
        });
    }
}
