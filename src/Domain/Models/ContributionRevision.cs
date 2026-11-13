using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Enums;

namespace Domain.Models;

public class ContributionRevision
{
    public Guid Id { get; } = Guid.Empty;
    public int RevisionNumber { get; private set; } = 0;
    public int AuthorId { get; private set; } = 0;
    public string Description { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; } = DateTime.Now;
    public string ReviewerNote { get; private set; } = string.Empty;
    public int? ChangedBy { get; private set; }
    public DateTime? ChangedAt { get; private set; }

    private readonly List<Evidence> _evidenceItems = new();
    public IReadOnlyList<Evidence> EvidenceItems => _evidenceItems;

    public ContributionRevision(int revisionNumber, int authorId, string description, IEnumerable<Evidence> evidenceItems)
    {
        Id = Guid.NewGuid();
        RevisionNumber = revisionNumber;
        AuthorId = authorId;
        Description = description ?? string.Empty;
        CreatedAt = DateTime.Now;

        if (evidenceItems != null)
        {
            _evidenceItems.AddRange(evidenceItems);
        }
    }

    public bool IsValid(out string error)
    {
        if(string.IsNullOrWhiteSpace(Description))
        {
            error = "Opis urađenog posla je obavezan.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public void MarkReviewed(int reviewerId)
    {
        ChangedBy = reviewerId;
        ChangedAt = DateTime.Now;
    }

    public bool SetReviewerNote(int reviewerId, string note, out string error)
    {
        if(string.IsNullOrWhiteSpace(note))
        {
            error = "Napomena nastavnika je obavezna.";
            return false;
        }

        ReviewerNote = note;
        MarkReviewed(reviewerId);
        error = string.Empty;
        return true;
    }
}
