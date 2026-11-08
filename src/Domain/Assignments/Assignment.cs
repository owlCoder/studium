namespace Domain.Assignments;

public sealed class Assignment
{
    public Guid OfferingId { get; }
    public string? Title { get; }
    public string? Description { get; }
    public string? Instructions { get; }
    public DateTimeOffset? Deadline { get; }
    public AssignmentStatus State { get; private set; } = AssignmentStatus.Draft;

    public Assignment(
        Guid offeringId,
        string? title,
        string? description,
        string? instructions,
        DateTimeOffset? deadline)
    {
        if (offeringId == Guid.Empty)
            throw new ArgumentException("Izvođenje predmeta mora biti zadato.", nameof(offeringId));

        OfferingId = offeringId;
        Title = title;
        Description = description;
        Instructions = instructions;
        Deadline = deadline;
    }

    public IReadOnlyList<string> GetMissingPublicationFields()
    {
        return AssignmentPublication.GetMissingPublicationFields(Title, Description, Instructions, Deadline);
    }

    public string? GetPublicationError()
    {
        return AssignmentPublication.GetPublicationError(
            isDraft: State == AssignmentStatus.Draft,
            title: Title,
            description: Description,
            instructions: Instructions,
            deadline: Deadline);
    }

    public bool IsVisibleToStudents()
    {
        return State == AssignmentStatus.Published;
    }

    public string? Publish()
    {
        var error = GetPublicationError();
        if (error is not null)
            return error;

        State = AssignmentStatus.Published;
        return null;
    }
}
