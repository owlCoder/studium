namespace Domain.Activity;

/// <summary>
/// Aktivnost vezana za kontekst u kome se izvodi (danas kataloški predmet, kasnije
/// izvođenje predmeta). Čuva samo identifikator konteksta, tako da domen ne zavisi
/// od načina skladištenja niti od drugih agregata.
/// Neispravni podaci se ne signaliziraju izuzetkom, već rezultatom
/// <see cref="ActivityCreation"/>.
/// </summary>
public sealed class Activity
{
    private Activity(
        Guid id,
        Guid contextId,
        string name,
        string description,
        DateTime startDate,
        DateTime endDate)
    {
        Id = id;
        ContextId = contextId;
        Name = name;
        Description = description;
        StartDate = startDate;
        EndDate = endDate;
    }

    public Guid Id { get; }
    public Guid ContextId { get; }
    public string Name { get; }
    public string Description { get; }
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }

    public static ActivityCreation TryCreate(
        Guid id,
        Guid contextId,
        string? name,
        string? description,
        DateTime startDate,
        DateTime endDate)
    {
        if (id == Guid.Empty)
        {
            return ActivityCreation.Failure("Identifikator mora biti zadat");
        }

        if (contextId == Guid.Empty)
        {
            return ActivityCreation.Failure("Identifikator konteksta mora biti zadat");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return ActivityCreation.Failure("Naziv aktivnosti mora biti zadat");
        }

        if (startDate >= endDate)
        {
            return ActivityCreation.Failure("Datum početka aktivnosti mora biti pre datuma završetka");
        }

        return ActivityCreation.Success(new Activity(
            id,
            contextId,
            name.Trim(),
            description ?? string.Empty,
            startDate,
            endDate));
    }
}
