namespace Application.Assignments;

public sealed class PublishAssignmentHandler
{
    private readonly IAssignmentRepository _repository;
    private readonly IAssignmentPublicationAccess _access;

    public PublishAssignmentHandler(
        IAssignmentRepository repository,
        IAssignmentPublicationAccess access)
    {
        _repository = repository;
        _access = access;
    }

    public async Task<PublishAssignmentResult> HandleAsync(
        Guid assignmentId,
        CancellationToken cancellationToken = default)
    {
        var assignment = await _repository.FindAsync(assignmentId, cancellationToken);

        if (assignment is null)
            return PublishAssignmentResult.Failure("Zadatak ne postoji.");

        if (!await _access.CanPublishAsync(assignment.OfferingId, cancellationToken))
            return PublishAssignmentResult.Failure("Nemate pravo da objavite zadatak na ovom izvođenju predmeta.");

        var publicationError = assignment.Publish();
        if (publicationError is not null)
            return PublishAssignmentResult.Failure(publicationError);

        await _repository.SaveAsync(assignment, cancellationToken);

        return PublishAssignmentResult.Success();
    }
}
