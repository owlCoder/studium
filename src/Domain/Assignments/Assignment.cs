namespace Domain.Assignments;

public sealed class Assignment
{
    public Guid Id { get; }
    public Guid CourseOfferingId { get; }
    public string Title { get; }
    public string? Description { get; private set; }
    public string? Instructions { get; }
    public DateTimeOffset? Deadline { get; }
    public AssignmentStatus Status { get; private set; } = AssignmentStatus.Draft;

    private Assignment(
        Guid id,
        Guid courseOfferingId,
        string title,
        string? description,
        string? instructions,
        DateTimeOffset? deadline)
    {
        Id = id;
        CourseOfferingId = courseOfferingId;
        Title = title;
        Description = description;
        Instructions = instructions;
        Deadline = deadline;
    }

    public static Assignment CreateDraft(
        Guid id,
        Guid courseOfferingId,
        string title,
        string? description,
        string? instructions,
        DateTimeOffset? deadline)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Identifikator zadatka je obavezan.", nameof(id));
        if (courseOfferingId == Guid.Empty)
            throw new ArgumentException("Izvođenje predmeta je obavezno.", nameof(courseOfferingId));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Naziv zadatka je obavezan.", nameof(title));

        return new Assignment(id, courseOfferingId, title, description, instructions, deadline);
    }

    public IReadOnlyList<string> GetMissingPublicationFields()
    {
        return AssignmentPublication.GetMissingPublicationFields(Title, Description, Instructions, Deadline);
    }

    public string? GetPublicationError()
    {
        return AssignmentPublication.GetPublicationError(
            isDraft: Status == AssignmentStatus.Draft,
            title: Title,
            description: Description,
            instructions: Instructions,
            deadline: Deadline);
    }

    public bool IsVisibleToStudents()
    {
        return Status == AssignmentStatus.Published;
    }

    public string? Publish()
    {
        var error = GetPublicationError();
        if (error is not null)
            return error;

        Status = AssignmentStatus.Published;
        return null;
    }

    /// <summary>
    /// Menja opis zadatka (ST-011, Izmena zadatka).
    /// Vraća null ako je izmena uspela, a poruku greške ako nije.
    /// </summary>
    public string? ChangeDescription(string? newDescription)
    {
        // 1. Opis može da se menja samo dok je zadatak Nacrt ili Objavljen.
        bool isDraft = Status == AssignmentStatus.Draft;
        bool isPublished = Status == AssignmentStatus.Published;
        if (!isDraft && !isPublished)
        {
            return "Opis može da se menja samo za zadatak u stanju Nacrt ili Objavljen.";
        }

        // 2. Objavljen zadatak ne sme da ostane bez opisa, jer je opis obavezan za objavu.
        if (isPublished && string.IsNullOrWhiteSpace(newDescription))
        {
            return "Objavljen zadatak mora imati opis.";
        }

        // 3. Sve provere su prošle, menjamo opis.
        Description = newDescription;
        return null;
    }
}
