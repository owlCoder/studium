using Application.Enrollments;
using Domain.Courses;
using Domain.Enrollments;
using Domain.Offerings;

namespace Infrastructure.Enrollments;

public sealed class InMemoryStudentOfferingsReader : IStudentOfferingsReader
{
    
    private readonly Dictionary<Guid, HashSet<Guid>> _activeOfferingsByStudent;

    
    private readonly Dictionary<Guid, Offering> _offeringsById;

    private readonly Dictionary<Guid, Course> _coursesById;

    public InMemoryStudentOfferingsReader(
        IEnumerable<Course> courses,
        IEnumerable<Offering> offerings,
        IEnumerable<Membership> memberships)
    {
        _coursesById = courses.ToDictionary(course => course.Id);
        _offeringsById = offerings.ToDictionary(offering => offering.Id);

        _activeOfferingsByStudent = memberships
            .Where(membership => membership.IsActive)
            .GroupBy(membership => membership.StudentId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(membership => membership.OfferingId).ToHashSet());
    }

    public Task<IReadOnlyList<EnrolledOfferingDto>> ListActiveForStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_activeOfferingsByStudent.TryGetValue(studentId, out var offeringIds))
        {
            return Task.FromResult<IReadOnlyList<EnrolledOfferingDto>>(Array.Empty<EnrolledOfferingDto>());
        }

        var result = new List<EnrolledOfferingDto>();
        foreach (var offeringId in offeringIds)
        {
            if (!_offeringsById.TryGetValue(offeringId, out var offering))
            {
                continue;
            }
            if (!_coursesById.TryGetValue(offering.CourseId, out var course))
            {
                continue;
            }

            result.Add(new EnrolledOfferingDto(offering.Id, course.Name, offering.AcademicYear));
        }

        return Task.FromResult<IReadOnlyList<EnrolledOfferingDto>>(result);
    }
}
