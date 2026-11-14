namespace Studium.Application.Reviews;

public record UpdateReviewDraftCommand(
    Guid DraftId,
    Guid TeacherId,
    string Comment,
    string Outcome
);

public class UpdateReviewDraftHandler
{
    private readonly IReviewRepository _repository;

    public UpdateReviewDraftHandler(IReviewRepository repository)
    {
        _repository = repository;
    }

    public async Task HandleAsync(UpdateReviewDraftCommand command, CancellationToken cancellationToken = default)
    {
        var draft = await _repository.GetByIdAsync(command.DraftId, cancellationToken);

        if (draft is null)
        {
            throw new KeyNotFoundException($"Nacrt pregleda sa ID-jem {command.DraftId} nije pronađen.");
        }

        if (draft.TeacherId != command.TeacherId)
        {
            throw new UnauthorizedAccessException("Nemate dozvolu da menjate ovaj nacrt.");
        }

        draft.UpdateDraft(command.Comment, command.Outcome);

        await _repository.UpdateAsync(draft, cancellationToken);
    }
}