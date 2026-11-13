using Domain.Staff;

namespace Application.Staff;

public sealed record AddCollaboratorCommand(Guid ActorId, Guid OfferingId, Guid UserId, Responsibility Responsibility);

public sealed record ListStaffQuery(Guid ActorId, Guid OfferingId);

// Bez lozinke i hash-a (spec 4.2)
public sealed record StaffMemberDto(
    Guid UserId,
    string Username,
    string FirstName,
    string LastName,
    Responsibility Responsibility);
