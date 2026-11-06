namespace Domain.Activity;

/// <summary>
/// Aktivnost vezana za konkretan predmet. Čuva samo identifikator predmeta,
/// tako da domen ne zavisi od načina skladištenja niti od drugih agregata.
/// </summary>
public sealed class Activity
{
    public Guid Id { get; }
    public Guid CourseId { get; }
    public string Name { get; }
    public string Description { get; }
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }

    public Activity(Guid id, Guid courseId, string name, string description, DateTime startDate, DateTime endDate)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Identifikator mora biti zadat", nameof(id));
        if (courseId == Guid.Empty)
            throw new ArgumentException("Identifikator predmeta mora biti zadat", nameof(courseId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Naziv aktivnosti mora biti zadat", nameof(name));
        if (startDate >= endDate)
            throw new ArgumentException("Datum početka aktivnosti mora biti pre datuma završetka", nameof(startDate));
        Id = id;
        CourseId = courseId;
        Name = name;
        Description = description ?? string.Empty;
        StartDate = startDate;
        EndDate = endDate;
    }
}
