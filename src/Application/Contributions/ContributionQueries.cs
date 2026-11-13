using Domain.Enums;
using Domain.Models;

namespace Application.Contributions;

public enum ViewerRole { Student, Teacher }

public record Viewer(int UserId, ViewerRole Role);

public class ContributionQueries
{
    private const string NotAvailable = "Doprinos ne postoji ili nemate pravo pristupa.";

    private readonly IContributionReader _reader;

    public ContributionQueries(IContributionReader reader) => _reader = reader;

    public IReadOnlyList<ContributionSummaryDto> List(Viewer viewer)
    {
        IReadOnlyList<Contribution> items = viewer.Role == ViewerRole.Teacher
            ? _reader.GetAll()
            : _reader.GetByAuthor(viewer.UserId);

        return items
            .OrderByDescending(c => c.LastChangedAt)
            .Select(ToSummary)
            .ToList();
    }

    public bool TryGetDetails(Viewer viewer, int id, out ContributionDetailsDto? result, out string error)
    {
        result = null;
        Contribution? c = _reader.GetById(id);

        // ista poruka za "ne postoji" i "tudji zapis", da se ne otkriva postojanje
        if (c is null || (viewer.Role == ViewerRole.Student && c.AuthorId != viewer.UserId))
        {
            error = NotAvailable;
            return false;
        }

        result = ToDetails(c);
        error = string.Empty;
        return true;
    }

    private static string StatusText(ContributionStatus status) => status switch
    {
        ContributionStatus.Podnet => "Podnet",
        ContributionStatus.VracenNaDopunu => "Vraćen na dopunu",
        _ => status.ToString()
    };

    private static ContributionSummaryDto ToSummary(Contribution c) =>
        new(c.Id, c.IdTask, c.AuthorId, c.PhaseId, StatusText(c.Status), c.CreatedAt, c.LastChangedAt);

    private static EvidenceDto ToEvidence(Evidence e) =>
        new(e.Id, e.IsLink ? "Veza" : "Datoteka", e.IsLink ? e.Link : e.FilePath, e.AddedAt);

    private static ContributionRevisionDto ToRevision(ContributionRevision r) =>
        new(r.RevisionNumber, r.Description, r.CreatedAt, r.ReviewerNote,
            r.EvidenceItems.Select(ToEvidence).ToList());

    private static ContributionDetailsDto ToDetails(Contribution c) =>
        new(c.Id, c.IdTask, c.AuthorId, c.PhaseId, StatusText(c.Status), c.CreatedAt, c.LastChangedAt,
            ToRevision(c.CurrentRevision),
            c.Revisions.Select(ToRevision).ToList());
}
