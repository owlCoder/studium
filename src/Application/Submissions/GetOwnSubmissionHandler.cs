using Domain.Submissions;

namespace Application.Submissions;

public sealed class GetOwnSubmissionHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly ISubmissionRepository _repository;

    public GetOwnSubmissionHandler(ICurrentUser currentUser, ISubmissionRepository repository)
    {
        _currentUser = currentUser;
        _repository = repository;
    }

    public async Task<Submission?> HandleAsync(Guid assignmentId)
    {
        var studentId = _currentUser.StudentId;
        if (studentId == Guid.Empty)
            throw new UnauthorizedAccessException("Za pregled predaje moraš biti prijavljen kao student.");

        if (assignmentId == Guid.Empty)
            throw new ArgumentException("Zadatak mora biti zadat.", nameof(assignmentId));

        var submission = await _repository.FindCurrentAsync(assignmentId, studentId);
        if (submission == null || submission.StudentId != studentId || submission.AssignmentId != assignmentId)
            return null;

        return submission;
    }
}
