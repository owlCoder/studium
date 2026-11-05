namespace Application.Groups;

public record AddStudentToGroupCommand(Guid GroupId, Guid StudentId, DateTime StartDate);

public interface IGroupMembershipRepository
{
    Task AddAsync(Domain.Entities.GroupMembership membership, CancellationToken cancellationToken);
    Task<Domain.Entities.GroupMembership?> GetActiveAsync(Guid groupId, Guid studentId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public class AddStudentToGroupHandler
{
    private readonly IGroupMembershipRepository _repository;

    public AddStudentToGroupHandler(IGroupMembershipRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(AddStudentToGroupCommand request, CancellationToken cancellationToken)
    {
        // Provera da li je student već aktivan u grupi
        var existing = await _repository.GetActiveAsync(request.GroupId, request.StudentId, cancellationToken);
        if (existing != null)
        {
            throw new InvalidOperationException("Student je već u ovoj grupi.");
        }

        var membership = new Domain.Entities.GroupMembership
        {
            Id = Guid.NewGuid(),
            GroupId = request.GroupId,
            StudentId = request.StudentId,
            StartDate = request.StartDate,
            EndDate = null
        };

        await _repository.AddAsync(membership, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}