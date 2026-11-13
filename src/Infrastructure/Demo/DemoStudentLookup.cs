using Application.Groups;

namespace Infrastructure.Demo;

public sealed class DemoStudentLookup : IStudentLookup
{
    private readonly IReadOnlyDictionary<Guid, bool> _students;

    public DemoStudentLookup()
        : this(DemoData.CreateStudentStatuses())
    {
    }

    public DemoStudentLookup(IReadOnlyDictionary<Guid, bool> students)
    {
        _students = students;
    }

    public Task<bool> IsActiveAsync(Guid studentId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var isActive = _students.TryGetValue(studentId, out var active) && active;
        return Task.FromResult(isActive);
    }
}
