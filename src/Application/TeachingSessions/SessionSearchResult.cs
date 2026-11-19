using Domain.TeachingSessions;
namespace Application.TeachingSessions;

public sealed record SessionSearchResult(
    IReadOnlyList<TeachingSession> Active,
    IReadOnlyList<TeachingSession> History);
