using Application.Access;
using Application.Interfaces;

namespace Application.Assignments;

public sealed class AssignmentCreationAccess(
    UserContext? currentUser,
    IOfferingAccessReader offeringAccess) : IAssignmentCreationAccess
{
    public async Task<bool> CanCreateAsync(
        Guid courseOfferingId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (currentUser is not { Role: UserRole.Teacher })
            return false;

        var isTeacherOfOffering = await offeringAccess.IsTeacherOfOfferingAsync(
            currentUser.UserId, courseOfferingId);

        cancellationToken.ThrowIfCancellationRequested();
        return isTeacherOfOffering;
    }
}
