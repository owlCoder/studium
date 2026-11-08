namespace Application.Assignments;

public sealed class PublishAssignmentHandler
{
    private readonly IAssignmentRepository _repository;

    public PublishAssignmentHandler(IAssignmentRepository repository)
    {
        _repository = repository;
    }

    public async Task<PublishAssignmentResult> HandleAsync(
        Guid assignmentId,
        CancellationToken cancellationToken = default)
    {
        var assignment = await _repository.FindAsync(assignmentId, cancellationToken);

        if (assignment is null)
            return PublishAssignmentResult.Failure("Zadatak ne postoji.");

        // TODO (kolega): Pre objave proveriti da li je trenutni korisnik nastavnik
        // angažovan na izvođenju predmeta kome ovaj zadatak pripada.

        var publicationError = assignment.Publish();
        if (publicationError is not null)
            return PublishAssignmentResult.Failure(publicationError);

        await _repository.SaveAsync(assignment, cancellationToken);

        return PublishAssignmentResult.Success();
    }
}
