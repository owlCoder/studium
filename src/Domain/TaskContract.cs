namespace Domain;

public record TaskContract(
    Guid TaskId,
    string Title,
    string Description,
    DateTime Deadline
);