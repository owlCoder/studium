namespace Application.StudentImport;

/// <summary>Кеш налога за један позив класификације.</summary>
/// <remarks>Памти и одговор да налог не постоји.</remarks>
internal sealed class AccountLookupCache : IAccountDirectory
{
    private readonly IAccountDirectory _inner;
    private readonly Dictionary<string, AccountSnapshot?> _cache =
        new(UsernameComparison.Comparer);

    public AccountLookupCache(IAccountDirectory inner) => _inner = inner;

    public Task<AccountSnapshot?> FindByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        var key = UsernameComparison.Key(username);
        if (_cache.TryGetValue(key, out var cached)) return Task.FromResult(cached);

        return FindAndRememberAsync(key, username, cancellationToken);
    }

    private async Task<AccountSnapshot?> FindAndRememberAsync(
        string key,
        string username,
        CancellationToken cancellationToken)
    {
        var result = await _inner.FindByUsernameAsync(username, cancellationToken);
        _cache[key] = result;
        return result;
    }

    public async Task<AccountRegistrationResult> TryAddAsync(
        NewUserAccount account,
        CancellationToken cancellationToken = default)
    {
        var result = await _inner.TryAddAsync(account, cancellationToken);

        // Освежава кеш и ако је налог у међувремену направио други позив.
        _cache[UsernameComparison.Key(account.Username)] = result.Account;
        return result;
    }
}
