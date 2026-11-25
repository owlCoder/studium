using System.Collections.Concurrent;
using Application.Submissions;
using Domain.Submissions;

namespace Infrastructure.Submissions;

public sealed class InMemorySubmissionRepository : ISubmissionRepository
{
    private readonly ConcurrentDictionary<(Guid AssignmentId, Guid StudentId), Submission> _submissions = new();

    public Task AddAsync(Submission submission)
    {
        ArgumentNullException.ThrowIfNull(submission);

        var key = (submission.AssignmentId, submission.StudentId);
        // Zamena predaje zahteva poseban tok. Dodavanje ne sme da prepiše važeću predaju.
        if (!_submissions.TryAdd(key, submission))
            throw new InvalidOperationException("Za ovaj zadatak i studenta već postoji važeća predaja.");

        return Task.CompletedTask;
    }

    public Task<Submission?> FindCurrentAsync(Guid assignmentId, Guid studentId)
    {
        return Task.FromResult(_submissions.GetValueOrDefault((assignmentId, studentId)));
    }
}
