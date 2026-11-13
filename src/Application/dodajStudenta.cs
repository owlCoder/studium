using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Groups;

public record AddStudentToGroupCommand(Guid GroupId, Guid StudentId, DateTime StartDate);

public interface IGroupMembershipRepository
{
    Task AddAsync(Domain.Entities.GroupMembership membership, CancellationToken cancellationToken);
    Task<Domain.Entities.GroupMembership?> GetActiveAsync(Guid groupId, Guid studentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Domain.Entities.GroupMembership>> GetActiveByStudentIdAsync(Guid studentId, CancellationToken cancellationToken); //1
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IGroupLookup
{
    Task<bool> ExistsAsync(Guid groupId, CancellationToken cancellationToken);
}

public interface IStudentLookup
{
    Task<bool> IsActiveAsync(Guid studentId, CancellationToken cancellationToken);
}

public class AddStudentToGroupHandler
{
    private readonly IGroupMembershipRepository _repository;
    private readonly IGroupLookup _groupLookup;
    private readonly IStudentLookup _studentLookup;

    public AddStudentToGroupHandler(
        IGroupMembershipRepository repository,
        IGroupLookup groupLookup,
        IStudentLookup studentLookup)
    {
        _repository = repository;
        _groupLookup = groupLookup;
        _studentLookup = studentLookup;
    }

    public async Task Handle(AddStudentToGroupCommand request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.GroupId == Guid.Empty)
        {
            throw new ArgumentException("ID grupe ne moze biti prazan!", nameof(request.GroupId));
        }

        if (request.StudentId == Guid.Empty)
        {
            throw new ArgumentException("ID studenta ne moze biti prazan!", nameof(request.StudentId));
        }

        if (request.StartDate == default)
        {
            throw new ArgumentException("Datum pocetka clanstva mora biti validan!", nameof(request.StartDate));
        }

        var groupExists = await _groupLookup.ExistsAsync(request.GroupId, cancellationToken);
        if (!groupExists)
        {
            throw new KeyNotFoundException("Navedena grupa ne postoji.");
        }

        var studentActive = await _studentLookup.IsActiveAsync(request.StudentId, cancellationToken);
        if (!studentActive)
        {
            throw new InvalidOperationException("Student ne postoji ili nije aktivan.");
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
