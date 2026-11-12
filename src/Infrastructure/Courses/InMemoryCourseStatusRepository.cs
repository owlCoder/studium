using System.Collections.Concurrent;
using Application.Courses;
using Domain.Courses;

namespace Infrastructure.Courses;


public sealed class InMemoryCourseStatusRepository : ICourseStatusRepository
{
    private readonly ConcurrentDictionary<Guid, Course> _coursesById;

    public InMemoryCourseStatusRepository(IEnumerable<Course> courses)
    {
        _coursesById = new ConcurrentDictionary<Guid, Course>(
            courses.ToDictionary(c => c.Id));
    }

    public Task<Course?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _coursesById.TryGetValue(id, out Course? course);
        return Task.FromResult(course);
    }

    public Task SaveAsync(Course course, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _coursesById[course.Id] = course;
        return Task.CompletedTask;
    }
}
