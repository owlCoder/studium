using System.Collections.Concurrent;
using Application.Assignments;
using Domain.Assignments;

namespace Infrastructure.Assignments;

public sealed class InMemoryAssignmentDraftWriter : IAssignmentDraftWriter, IAssignmentRepository
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

    public Task<Assignment?> FindAsync(
        Guid assignmentId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(FindById(assignmentId));
    }

    public Task SaveAsync(
        Assignment assignment,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(assignment);

        _drafts[assignment.Id] = assignment;
        return Task.CompletedTask;
    }
}
