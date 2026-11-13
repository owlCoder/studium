using System.Collections.Concurrent;
using Application.Assignments;
using Domain.Assignments;

namespace Infrastructure.Assignments;

public sealed class InMemoryAssignmentDraftWriter : IAssignmentDraftWriter
{
    private readonly ConcurrentDictionary<Guid, Assignment> _drafts = new();

    public Task SaveAsync(
        Guid assignmentId,
        CreateAssignmentCommand command,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(command);

        var draft = Assignment.CreateDraft(
            assignmentId,
            command.CourseOfferingId,
            command.Title ?? string.Empty,
            command.Description,
            command.Instructions,
            command.Deadline);

        if (!_drafts.TryAdd(draft.Id, draft))
            throw new InvalidOperationException("Zadatak sa tim identifikatorom već postoji.");

        return Task.CompletedTask;
    }

    public Assignment? FindById(Guid assignmentId) => _drafts.GetValueOrDefault(assignmentId);
}
