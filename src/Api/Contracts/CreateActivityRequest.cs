namespace Api.Contracts;

public sealed record CreateActivityRequest(
    string Name,
    string Description,
    DateTime StartDate,
    DateTime EndDate);
