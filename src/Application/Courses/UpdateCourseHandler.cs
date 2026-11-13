
using System;
using System.Threading;
using System.Threading.Tasks;
using Domain.Courses;

namespace Application.Courses;

public sealed class UpdateCourseHandler
{
    private readonly ICourseRepository _repository;
    private readonly ICourseOfferingLookup _offeringLookup;

    public UpdateCourseHandler(ICourseRepository repository, ICourseOfferingLookup offeringLookup)
    {
        _repository = repository;
        _offeringLookup = offeringLookup;
    }

    public async Task HandleAsync(UpdateCourseCommand command, CancellationToken cancellationToken = default)
    {
        var course = await _repository.GetByIdAsync(command.CourseId, cancellationToken);

        if (course == null)
            throw new KeyNotFoundException($"Predmet sa ID {command.CourseId} nije pronađen.");

        string normalizedCode = command.Code.Trim().ToUpperInvariant();

        if (!string.Equals(course.Code, normalizedCode, StringComparison.OrdinalIgnoreCase) &&
            await _repository.ExistsByCodeAsync(normalizedCode, cancellationToken))
        {
            throw new InvalidOperationException($"Predmet sa oznakom {normalizedCode} već postoji.");
        }

        course.Update(command.Code, command.Name, command.Description, _offeringLookup);

        await _repository.UpdateAsync(course, cancellationToken);
    }
}
