using Application.Courses;
using Domain.Activity;

namespace Application.Activities;

/// <summary>
/// Dodaje aktivnost postojećem predmetu. Katalog predmeta se ne menja; postojeći
/// port <see cref="ICourseReader"/> služi samo za proveru da predmet postoji.
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
        if (!courses.Any(course => course.Id == command.CourseId))
            return CreateActivityResult.CourseNotFound(command.CourseId);

        Activity activity;
        try
        {
            activity = new Activity(
                Guid.NewGuid(),
                command.CourseId,
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
