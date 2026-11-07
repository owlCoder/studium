namespace Domain.Academic;

/// <summary>
/// Izvođenje predmeta u jednoj akademskoj godini (ST-004).
/// Par (CourseId, AcademicYearId) jednoznačno određuje izvođenje.
/// </summary>
public sealed class CourseOffering
{
    public Guid Id { get; }
    public Guid CourseId { get; }
    public Guid AcademicYearId { get; }
    public OfferingState State { get; private set; }
    public bool IsArchived { get; private set; }

    public CourseOffering(Guid id, Guid courseId, Guid academicYearId)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Identifikator mora biti zadat.", nameof(id));
        }

        if (courseId == Guid.Empty)
        {
            throw new ArgumentException("Predmet mora biti zadat.", nameof(courseId));
        }

        if (academicYearId == Guid.Empty)
        {
            throw new ArgumentException("Akademska godina mora biti zadata.", nameof(academicYearId));
        }

        Id = id;
        CourseId = courseId;
        AcademicYearId = academicYearId;
        State = OfferingState.InPreparation;
        IsArchived = false;
    }

    /// <summary>
    /// Dozvoljeni prelazi: U pripremi → Aktivno → Završeno. Nazad nije dozvoljeno.
    /// </summary>
    public void ChangeState(OfferingState newState)
    {
        bool allowed =
            (State == OfferingState.InPreparation && newState == OfferingState.Active) ||
            (State == OfferingState.Active && newState == OfferingState.Completed);

        if (!allowed)
        {
            throw new InvalidOperationException(
                $"Prelaz iz stanja {State} u stanje {newState} nije dozvoljen.");
        }

        State = newState;
    }
}
