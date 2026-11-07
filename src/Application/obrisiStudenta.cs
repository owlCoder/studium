namespace Application.Groups;

public record RemoveStudentFromGroupCommand(Guid GroupId, Guid StudentId, DateTime EndDate);

public class RemoveStudentFromGroupHandler
{
    private readonly IGroupMembershipRepository _repository;

    public RemoveStudentFromGroupHandler(IGroupMembershipRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(RemoveStudentFromGroupCommand request, CancellationToken cancellationToken)
    {
        var membership = await _repository.GetActiveAsync(request.GroupId, request.StudentId, cancellationToken);
        if (membership == null)
        {
            throw new KeyNotFoundException("Aktivno članstvo studenta u ovoj grupi nije pronađeno.");
        }

        membership.EndDate = request.EndDate;

        await _repository.SaveChangesAsync(cancellationToken);
    }
}
