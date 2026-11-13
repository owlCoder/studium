namespace Domain.Academic;

/// <summary>
/// Akademska godina (ST-004): oznaka i granični datumi.
/// </summary>
public sealed class AcademicYear
{
    public Guid Id { get; }
    public string Label { get; }
    public DateTime StartsAt { get; }
    public DateTime EndsAt { get; }

    public AcademicYear(Guid id, string label, DateTime startsAt, DateTime endsAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Identifikator mora biti zadat.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ArgumentException("Oznaka akademske godine je obavezna.", nameof(label));
        }

        if (startsAt >= endsAt)
        {
            throw new ArgumentException("Početak godine mora prethoditi završetku.", nameof(startsAt));
        }

        Id = id;
        Label = label.Trim();
        StartsAt = startsAt;
        EndsAt = endsAt;
    }
}
