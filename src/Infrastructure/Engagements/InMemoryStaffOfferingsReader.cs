using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Engagements;
using Domain.Courses;
using Domain.Offerings;
using Domain.Staff;

namespace Infrastructure.Engagements;

public sealed class InMemoryStaffOfferingsReader : IStaffOfferingsReader
{
    // Indeks na user_id -> aktivna angažovanja tog korisnika.
    private readonly Dictionary<Guid, TeachingAssignment[]> _activeAssignmentsByUser;

    private readonly Dictionary<Guid, Offering> _offeringsById;
    private readonly Dictionary<Guid, Course> _coursesById;

    public InMemoryStaffOfferingsReader(
        IEnumerable<Course> courses,
        IEnumerable<Offering> offerings,
        IEnumerable<TeachingAssignment> assignments)
    {
        _coursesById = courses.ToDictionary(course => course.Id);
        _offeringsById = offerings.ToDictionary(offering => offering.Id);

        _activeAssignmentsByUser = assignments
            .Where(assignment => assignment.IsActive)
            .GroupBy(assignment => assignment.UserId)
            .ToDictionary(group => group.Key, group => group.ToArray());
    }

    public Task<IReadOnlyList<TeachingOfferingDto>> ListActiveForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_activeAssignmentsByUser.TryGetValue(userId, out var assignments))
        {
            return Task.FromResult<IReadOnlyList<TeachingOfferingDto>>(Array.Empty<TeachingOfferingDto>());
        }

        var result = new List<TeachingOfferingDto>();
        foreach (var assignment in assignments)
        {
            if (!_offeringsById.TryGetValue(assignment.OfferingId, out var offering))
            {
                continue;
            }
            if (!_coursesById.TryGetValue(offering.CourseId, out var course))
            {
                continue;
            }

            result.Add(new TeachingOfferingDto(
                offering.Id, course.Name, offering.AcademicYear, assignment.Responsibility));
        }

        return Task.FromResult<IReadOnlyList<TeachingOfferingDto>>(result);
    }
}
