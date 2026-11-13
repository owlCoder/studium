namespace Domain.Assignments;

public sealed class Assignment
{
    public Guid Id { get; }
    public Guid CourseOfferingId { get; }
    public string Title { get; }
    public string? Description { get; }
    public string? Instructions { get; }
    public DateTimeOffset? Deadline { get; }
    public AssignmentStatus Status { get; } = AssignmentStatus.Draft;

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
}
