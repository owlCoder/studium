using System;
using System.Threading;
using System.Threading.Tasks;

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
        if(request==null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if(request.GroupId==Guid.Empty)
        {
            throw new ArgumentException("ID grupe ne moze biti prazan!",nameof(request.GroupId));
        }

        if (request.StudentId == Guid.Empty)
        {
            throw new ArgumentException("ID studenta ne moze biti prazan!", nameof(request.StudentId));
        }
        if (request.StartDate == default)
        {
            throw new ArgumentException("Datum pocetka clanstva mora biti validan!", nameof(request.StartDate));
        }

        var existing = await _repository.GetActiveAsync(request.GroupId, request.StudentId, cancellationToken);
        if (existing != null)
        {
            throw new InvalidOperationException("Student je vec u ovoj grupi.");
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