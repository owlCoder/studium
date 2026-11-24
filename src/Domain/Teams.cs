using System.Collections.ObjectModel;

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
/// Stanje tima se menja samo kroz domenske operacije: sva svojstva imaju privatan
/// seter, a liste članova i poziva se izlažu samo kao kolekcije samo za čitanje.
/// </summary>
public class StudentTeam
{
    private readonly List<Guid> _memberStudentIds = new();
    private readonly List<Guid> _pendingInvitationStudentIds = new();
    private readonly ReadOnlyCollection<Guid> _memberStudentIdsView;
    private readonly ReadOnlyCollection<Guid> _pendingInvitationStudentIdsView;

    private StudentTeam()
    {
        _memberStudentIdsView = _memberStudentIds.AsReadOnly();
        _pendingInvitationStudentIdsView = _pendingInvitationStudentIds.AsReadOnly();
    }

    /// <summary>
    /// Identifikator tima. Dodeljuje se pri kreiranju i posle se ne menja.
    /// </summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid CourseOfferingId { get; private set; }

    public Guid ProjectActivityId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public int MinSize { get; private set; }

    public int MaxSize { get; private set; }

    /// <summary>
    /// Članovi tima, uključujući tvorca. Kolekcija je samo za čitanje; članstvo se menja
    /// isključivo domenskim operacijama koje poštuju pravila tima.
    /// </summary>
    public IReadOnlyCollection<Guid> MemberStudentIds => _memberStudentIdsView;

    /// <summary>
    /// Studenti koji su pozvani u tim, a još nisu članovi. Kolekcija je samo za čitanje.
    /// </summary>
    public IReadOnlyCollection<Guid> PendingInvitationStudentIds => _pendingInvitationStudentIdsView;

    public TeamCompositionStatus Status { get; private set; } = TeamCompositionStatus.Open;

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

        var team = new StudentTeam
        {
            CourseOfferingId = courseOfferingId,
            ProjectActivityId = projectActivityId,
            Name = name.Trim(),
            MinSize = minSize,
            MaxSize = maxSize,
            Status = TeamCompositionStatus.Open
        };
        team._memberStudentIds.Add(creatorStudentId);

        return TeamCreation.Success(team);
    }
}
