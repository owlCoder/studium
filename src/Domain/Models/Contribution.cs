using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Enums;

namespace Domain.Models;

public class Contribution
{
    public int Id { get; set; } = 0;
    public int IdTask { get; set; } = 0;
    public int AuthorId { get; private set; } = 0;
    public int PhaseId { get; set; } = 0;
    public ContributionStatus Status { get; private set; } = ContributionStatus.Podnet;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime LastChangedAt { get; set; } = DateTime.Now;

    private readonly List<ContributionRevision> _revisions = new();
    public IReadOnlyList<ContributionRevision> Revisions => _revisions;

    public ContributionRevision CurrentRevision => _revisions.Last();

    public Contribution(int id, int idTask, int authorId, int phaseId, string description, IEnumerable<Evidence> evidenceItems)
    {
        Id = id;
        IdTask = idTask;
        AuthorId = authorId;
        PhaseId = phaseId;
        Status = ContributionStatus.Podnet;
        CreatedAt = DateTime.Now;
        LastChangedAt = CreatedAt;

        _revisions.Add(new ContributionRevision(1, 1, authorId, description, evidenceItems));
    }

    public bool ReturnForSupplement(string reviewerNote, out string error)
    {
        if(Status != ContributionStatus.Podnet)
        {
            error = "Samo podnet doprinos može biti vraćen na dopunu!";
            return false;
        }

        if (!CurrentRevision.SetReviewerNote(reviewerNote, out error))
        {
            return false;
        }

        Status = ContributionStatus.VracenNaDopunu;
        LastChangedAt = DateTime.Now;
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
        ContributionRevision revision = new ContributionRevision(next, next, editorId, newDescription, evidenceItems);

        if (!revision.IsValid(out error))
        {
            return false;
        }

        _revisions.Add(revision);
        Status = ContributionStatus.Podnet;
        LastChangedAt = DateTime.Now;
        return true;
    }
}
