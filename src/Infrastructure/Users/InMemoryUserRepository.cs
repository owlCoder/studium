using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Access;
using Application.Auth;
using Application.Users;
using Domain.Users;

namespace Infrastructure.Users;

public sealed class InMemoryUserRepository :
    IUserRepository,
    IUserLookup,
    IAccountReader
{
    private readonly object _gate = new();

    private readonly Dictionary<Guid, User> _usersById = new();

    private readonly Dictionary<string, Guid> _idsByUsername = new();

    public InMemoryUserRepository(IEnumerable<User>? initialUsers = null)
    {
        if (initialUsers == null)
        {
            return;
        }

        foreach (var user in initialUsers)
        {
            AddCore(user);
        }
    }

    // IUserRepository: pronalaženje po ID-u.
    public Task<User?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            _usersById.TryGetValue(id, out var user);
            return Task.FromResult(user?.Copy());
        }
    }

    // IUserRepository: pronalaženje po korisničkom imenu.
    public Task<User?> GetByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        return FindByUsernameAsync(username, cancellationToken);
    }

    // IUserLookup: koristi ga LoginHandler.
    public Task<User?> FindByUsernameAsync(
        string normalizedUsername,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(normalizedUsername))
        {
            return Task.FromResult<User?>(null);
        }

        string normalized = User.NormalizeUsername(normalizedUsername);

        lock (_gate)
        {
            if (_idsByUsername.TryGetValue(normalized, out var id))
            {
                return Task.FromResult<User?>(_usersById[id].Copy());
            }

            return Task.FromResult<User?>(null);
        }
    }

    // IAccountReader: koristi ga provera autorizacije.
    // Vraća samo podatke potrebne za odluku o pristupu.
    public Task<AccountInfo?> FindByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (!_usersById.TryGetValue(id, out var user))
            {
                return Task.FromResult<AccountInfo?>(null);
            }

            var account = new AccountInfo(
                user.Id,
                user.Role,
                user.IsActive,
                user.IsActivated);

            return Task.FromResult<AccountInfo?>(account);
        }
    }

    // IUserRepository: dodavanje korisnika.
    public Task AddAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        lock (_gate)
        {
            AddCore(user);
        }

        return Task.CompletedTask;
    }

    // IUserRepository: ažuriranje korisnika.
    public Task UpdateAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        lock (_gate)
        {
            if (!_usersById.TryGetValue(user.Id, out var existing))
            {
                throw new InvalidOperationException(
                    "Korisnik ne postoji.");
            }

            if (user.Status != existing.Status)
            {
                throw new InvalidOperationException(
                    "Stanje naloga može se menjati samo preko DeactivateAsync.");
            }

            if (_idsByUsername.TryGetValue(user.Username, out var ownerId)
                && ownerId != user.Id)
            {
                throw new InvalidOperationException(
                    "Korisničko ime je već zauzeto.");
            }

            _idsByUsername.Remove(existing.Username);
            _usersById[user.Id] = user.Copy();
            _idsByUsername[user.Username] = user.Id;
        }

        return Task.CompletedTask;
    }

    // Atomska deaktivacija: provera i promena stanja izvrsavaju se
    // unutar istog lock-a.
    public Task<DeactivateUserResult> DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (!_usersById.TryGetValue(id, out var user))
            {
                return Task.FromResult(DeactivateUserResult.NotFound);
            }

            if (!user.IsActive)
            {
                return Task.FromResult(
                    DeactivateUserResult.AlreadyDeactivated);
            }

            if (user.Role == UserRole.Administrator)
            {
                int activeAdministrators = 0;

                foreach (var candidate in _usersById.Values)
                {
                    if (candidate.Role == UserRole.Administrator
                        && candidate.IsActive)
                    {
                        activeAdministrators++;
                    }
                }

                if (activeAdministrators <= 1)
                {
                    return Task.FromResult(
                        DeactivateUserResult.LastActiveAdministrator);
                }
            }

            user.Deactivate();

            return Task.FromResult(DeactivateUserResult.Deactivated);
        }
    }

    // Poziva se samo unutar lock-a ili iz konstruktora.
    private void AddCore(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (_usersById.ContainsKey(user.Id))
        {
            throw new InvalidOperationException(
                "Korisnik sa istim ID-em već postoji.");
        }

        if (_idsByUsername.ContainsKey(user.Username))
        {
            throw new InvalidOperationException(
                "Korisničko ime je već zauzeto.");
        }

        _usersById[user.Id] = user.Copy();
        _idsByUsername[user.Username] = user.Id;
    }
}
