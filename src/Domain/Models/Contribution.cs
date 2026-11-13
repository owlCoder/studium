using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Enums;

namespace Domain.Models;

public class Contribution
{
    public Guid Id { get; } = Guid.Empty;
    public int IdTask { get; set; } = 0;
    public int AuthorId { get; private set; } = 0;
    public int PhaseId { get; set; } = 0;
    public ContributionStatus Status { get; private set; } = ContributionStatus.Podnet;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime ChangedAt { get; set; } = DateTime.Now;

    private readonly List<ContributionRevision> _revisions = new();
    public IReadOnlyList<ContributionRevision> Revisions => _revisions;

    public ContributionRevision CurrentRevision => _revisions.Last();

    public Contribution(int idTask, int authorId, int phaseId, string description, IEnumerable<Evidence> evidenceItems)
    {
        Id = Guid.NewGuid();
        IdTask = idTask;
        AuthorId = authorId;
        PhaseId = phaseId;
        Status = ContributionStatus.Podnet;
        CreatedAt = DateTime.Now;
        ChangedAt = CreatedAt;

        _revisions.Add(new ContributionRevision(1, authorId, description, evidenceItems));
    }

    private bool IsInStatus(ContributionStatus expected, string message, out string error)
    {
        bool matches = Status == expected;
        error = matches ? string.Empty : message;
        return matches;
    }

    private void ChangeStatus(ContributionStatus newStatus)
    {
        Status = newStatus;
        ChangedAt = DateTime.Now;
    }

    public bool Confirm(int reviewerId, string reviewerNote, out string error)
    {
        if (!IsInStatus(ContributionStatus.Podnet, "Samo podnet doprinos može biti potvrđen!", out error))
            return false;

        if (!CurrentRevision.SetReviewerNote(reviewerId, reviewerNote, out error))
            return false;

        ChangeStatus(ContributionStatus.Potvrdjen);
        return true;
    }

    public bool ReturnForSupplement(int reviewerId, string reviewerNote, out string error)
    {
        if (!IsInStatus(ContributionStatus.Podnet, "Samo podnet doprinos može biti vraćen na dopunu!", out error))
            return false;

        if (!CurrentRevision.SetReviewerNote(reviewerId, reviewerNote, out error))
            return false;

        ChangeStatus(ContributionStatus.VracenNaDopunu);
        return true;
    }

    public bool Supplement(int editorId, string newDescription, IEnumerable<Evidence> evidenceItems, out string error)
    {
        if(editorId != AuthorId)
        {
            error = "Samo autor moze da dopuni svoj doprinos!";
            return false;
        }

        if(Status != ContributionStatus.VracenNaDopunu)
        {
            error = "Doprinos se može dopuniti samo kada je vraćen na dopunu!";
            return false;
        }

        int next = _revisions.Count + 1;
        ContributionRevision revision = new ContributionRevision(next, editorId, newDescription, evidenceItems);

        if (!revision.IsValid(out error))
        {
            return false;
        }

        _revisions.Add(revision);
        Status = ContributionStatus.Podnet;
        ChangedAt = DateTime.Now;
        return true;
    }
}
