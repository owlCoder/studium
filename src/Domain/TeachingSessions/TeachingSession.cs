namespace Domain.TeachingSessions;

/// <summary>
/// Termin nastave (ST-031). Nastaje u stanju <see cref="SessionState.Planiran"/>.
/// </summary>
public sealed class TeachingSession
{
    public Guid Id { get; }
    public string Kind { get; }
    public Guid OfferingId { get; }

    /// <summary>
    /// Ciljna grupa, ili <c>null</c> kada je cilj celo izvođenje.
    /// </summary>
    public Guid? GroupId { get; }

    public DateTime StartsAt { get; }
    public DateTime EndsAt { get; }
    public string Location { get; }
    public SessionState State { get; private set; }

    public bool TargetsWholeOffering => GroupId is null;

    public TeachingSession(
        Guid id,
        string kind,
        Guid offeringId,
        Guid? groupId,
        DateTime startsAt,
        DateTime endsAt,
        string location)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Identifikator mora biti zadat.", nameof(id));
        if (string.IsNullOrWhiteSpace(kind))
            throw new ArgumentException("Vrsta nastave je obavezna.", nameof(kind));
        if (offeringId == Guid.Empty)
            throw new ArgumentException("Izvođenje mora biti zadato.", nameof(offeringId));
        if (endsAt <= startsAt)
            throw new ArgumentException(
                "Završetak termina mora biti kasniji od početka.",
                nameof(endsAt));

        Id = id;
        Kind = kind.Trim();
        OfferingId = offeringId;
        GroupId = groupId;
        StartsAt = startsAt;
        EndsAt = endsAt;
        Location = location?.Trim() ?? string.Empty;
        State = SessionState.Planiran;
    }

    public void MarkHeld()
    {
        if (State == SessionState.Otkazan)
            throw new InvalidOperationException(
                "Otkazan termin se ne može označiti kao održan.");

        State = SessionState.Odrzan;
    }

    public void Cancel()
    {
        if (State == SessionState.Odrzan)
            throw new InvalidOperationException(
                "Održan termin se ne može otkazati.");

        State = SessionState.Otkazan;
    }
}
