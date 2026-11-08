using Application.Academic;
using Domain.Academic;
using Domain.Staff;

namespace Application.Staff;

/// <summary>ST-005 (saradnici): odgovorni nastavnik dodaje nastavnika ili asistenta.</summary>
public sealed class AddCollaboratorHandler
{
    private readonly ICourseOfferingRepository _offerings;
    private readonly ITeachingAssignmentRepository _assignments;
    private readonly IUserDirectory _users;
    private readonly ITeachingStaffQueries _staff;

    public AddCollaboratorHandler(
        ICourseOfferingRepository offerings,
        ITeachingAssignmentRepository assignments,
        IUserDirectory users,
        ITeachingStaffQueries staff)
    {
        _offerings = offerings;
        _assignments = assignments;
        _users = users;
        _staff = staff;
    }

    public async Task<StaffMemberDto> HandleAsync(
        AddCollaboratorCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.Responsibility == Responsibility.ResponsibleTeacher)
        {
            throw new ArgumentException("Odgovornog nastavnika dodeljuje administrator.", nameof(command));
        }

        var offering = await _offerings.GetByIdAsync(command.OfferingId, cancellationToken)
            ?? throw new NotFoundException("Izvođenje ne postoji.");

        // Ovlašćenje se proverava nad konkretnim izvođenjem, ne samo po ulozi.
        if (!await _staff.IsResponsibleTeacherAsync(command.ActorId, command.OfferingId, cancellationToken))
        {
            throw new AccessDeniedException("Samo odgovorni nastavnik ovog izvođenja može dodavati saradnike.");
        }

        if (offering.IsArchived || offering.State == OfferingState.Completed)
        {
            throw new InvalidOperationException("Završeno ili arhivirano izvođenje ne prima izmene.");
        }

        var user = await _users.GetByIdAsync(command.UserId, cancellationToken)
            ?? throw new NotFoundException("Korisnik ne postoji.");

        if (!user.IsActive)
        {
            throw new InvalidOperationException("Korisnički nalog je deaktiviran.");
        }

        bool roleMatches =
            (command.Responsibility == Responsibility.Teacher && user.Role == UserRole.Teacher) ||
            (command.Responsibility == Responsibility.Assistant && user.Role == UserRole.Assistant);

        if (!roleMatches)
        {
            throw new InvalidOperationException("Uloga naloga ne odgovara traženoj odgovornosti.");
        }

        var existing = await _assignments.GetActiveAsync(command.OfferingId, command.UserId, cancellationToken);
        if (existing is null)
        {
            existing = new TeachingAssignment(
                Guid.NewGuid(), command.OfferingId, command.UserId, command.Responsibility);
            await _assignments.AddAsync(existing, cancellationToken);
        }
        // već aktivan: vraća se postojeći, bez drugog reda

        return new StaffMemberDto(user.Id, user.Username, user.FirstName, user.LastName, existing.Responsibility);
    }
}
