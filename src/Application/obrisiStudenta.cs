using Domain.Entities;

namespace Application.Groups;

public record RemoveStudentFromGroupCommand(Guid GroupId, Guid StudentId, DateTime EndDate);

public class RemoveStudentFromGroupHandler
{
    private readonly IGroupMembershipRepository _repository;

    public RemoveStudentFromGroupHandler(IGroupMembershipRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(RemoveStudentFromGroupCommand request, CancellationToken cancellationToken = default)
    {
        var membership = await _repository.GetActiveAsync(request.GroupId, request.StudentId, cancellationToken);
        if (membership == null)
        {
            throw new KeyNotFoundException("Aktivno clanstvo studenta u ovoj grupi nije pronadjeno");
        }

        if (request.EndDate < membership.StartDate)
        {
            throw new ArgumentException("Datum zavrsetka clanstva ne moze biti pre datuma pocetka");
        }

        membership.EndDate = request.EndDate;
        await _repository.SaveChangesAsync(cancellationToken);
    }
}