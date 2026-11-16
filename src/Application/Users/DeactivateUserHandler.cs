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

    public Task<DeactivateUserResult> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _userRepository.DeactivateAsync(id, cancellationToken);
    }
}
