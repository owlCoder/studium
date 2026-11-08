namespace Domain.Assignments;

public sealed class Assignment
{
    public string? Title { get; }
    public string? Description { get; }
    public string? Instructions { get; }
    public DateTimeOffset? Deadline { get; }
    public AssignmentStatus State { get; private set; } = AssignmentStatus.Draft;

    public Assignment(string? title, string? description, string? instructions, DateTimeOffset? deadline)
    {
        Title = title;
        Description = description;
        Instructions = instructions;
        Deadline = deadline;
    }

    public string? Publish()
    {
        var error = AssignmentPublication.GetPublicationError(
            isDraft: State == AssignmentStatus.Draft,
            title: Title,
            description: Description,
            instructions: Instructions,
            deadline: Deadline);

        if (error is not null)
            return error;

        State = AssignmentStatus.Published;
        return null;
    }
}
