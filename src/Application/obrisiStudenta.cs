using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Groups;

public record RemoveStudentFromGroupCommand(Guid GroupId, Guid StudentId, DateTime EndDate);

public class RemoveStudentFromGroupHandler
{
    private readonly IGroupMembershipRepository _repository;

    public RemoveStudentFromGroupHandler(IGroupMembershipRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task Handle(RemoveStudentFromGroupCommand request, CancellationToken cancellationToken)
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

        if (request.EndDate == default)
        {
            throw new ArgumentException("Datum zavrsetka mora biti validan!", nameof(request.EndDate));
        }

        var membership = await _repository.GetActiveAsync(request.GroupId, request.StudentId, cancellationToken);
        if (membership == null)
        {
            throw new KeyNotFoundException("Aktivno clanstvo studenta u ovoj grupi nije pronadjeno.");
        }

        membership.Remove(request.EndDate);

        await _repository.SaveChangesAsync(cancellationToken);
    }
}
