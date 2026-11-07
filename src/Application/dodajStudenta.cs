using Domain.Entities;

namespace Application.Groups;

public record AddStudentToGroupCommand(Guid GroupId, Guid StudentId, DateTime StartDate);

public class AddStudentToGroupHandler
{
    private readonly IGroupMembershipRepository _repository;
    private readonly IGroupLookup? _groupLookup;
    private readonly IStudentLookup? _studentLookup;

    public AddStudentToGroupHandler(
        IGroupMembershipRepository repository,
        IGroupLookup? groupLookup = null,
        IStudentLookup? studentLookup = null)
    {
        _repository = repository;
        _groupLookup = groupLookup;
        _studentLookup = studentLookup;
    }

    public async Task Handle(AddStudentToGroupCommand request, CancellationToken cancellationToken = default)
    {
        if (_groupLookup != null && !await _groupLookup.ExistsAsync(request.GroupId, cancellationToken))
        {
            throw new KeyNotFoundException("Navedena grupa ne postoji");
        }

        if (_studentLookup != null && !await _studentLookup.IsActiveAsync(request.StudentId, cancellationToken))
        {
            throw new InvalidOperationException("Student ne postoji ili nije aktivan");
        }

        var existingActive = await _repository.GetActiveAsync(request.GroupId, request.StudentId, cancellationToken);
        if (existingActive != null)
        {
            throw new InvalidOperationException("Student je vec u ovoj grupi");
        }

        var membership = new GroupMembership
        {
            Id = Guid.NewGuid(),
            GroupId = request.GroupId,
            StudentId = request.StudentId,
            StartDate = request.StartDate
        };

        try
        {
            await _repository.AddAsync(membership, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            throw new InvalidOperationException("Student je vec u ovoj grupi");
        }
    }
}