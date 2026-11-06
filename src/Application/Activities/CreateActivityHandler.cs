using Application.Common;
using Application.Courses;
using Domain.Activity;

namespace Application.Activities;

/// <summary>
/// Dodaje aktivnost postojećem predmetu. Katalog predmeta se ne menja; postojeći port
/// <see cref="ICourseReader"/> služi samo da se predmet pronađe po oznaci oblika
/// <c>OZNAKA-xxxxx</c> (vidi <see cref="EntitySlug"/>).
/// </summary>
public sealed class CreateActivityHandler
{
    private readonly IActivityRepository _repository;
    private readonly ICourseReader _courseReader;

    public CreateActivityHandler(IActivityRepository repository, ICourseReader courseReader)
    {
        _repository = repository;
        _courseReader = courseReader;
    }

    public async Task<CreateActivityResult> HandleAsync(
        CreateActivityCommand command,
        CancellationToken cancellationToken = default)
    {
        var courses = await _courseReader.ListAsync(cancellationToken);
        var matches = courses
            .Where(course => EntitySlug.Matches(command.CourseSlug, course.Code, course.Id))
            .ToList();

        if (matches.Count == 0)
        {
            return CreateActivityResult.CourseNotFound(command.CourseSlug);
        }

        if (matches.Count > 1)
        {
            return CreateActivityResult.InvalidRequest(
                $"Oznaka predmeta \"{command.CourseSlug}\" nije jedinstvena.");
        }

        Activity activity;
        try
        {
            activity = new Activity(
                Guid.NewGuid(),
                matches[0].Id,
                command.Name,
                command.Description,
                command.StartDate,
                command.EndDate);
        }
        catch (ArgumentException exception)
        {
            return CreateActivityResult.InvalidRequest(exception.Message);
        }

        await _repository.AddAsync(activity, cancellationToken);
        return CreateActivityResult.Created(ActivityListItem.From(activity));
    }
}
