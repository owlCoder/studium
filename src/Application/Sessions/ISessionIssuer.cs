namespace Application.Sessions;

/// <summary>
/// Izdavanje sesije za već proverenog korisnika.
/// </summary>
public interface ISessionIssuer
{
    Task<IssuedSession> IssueAsync(Guid userId,
        CancellationToken cancellationToken = default);
}
