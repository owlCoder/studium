using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Users;

public sealed class DeactivateUserHandler
{
    private readonly IUserRepository _userRepository;

    public DeactivateUserHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user == null)
        {
            throw new ArgumentException($"Korisnik sa ID-jem {id} nije pronađen.");
        }

        user.Deactivate();
        await _userRepository.UpdateAsync(user, cancellationToken);
    }
}
