using Application.Common;
using Application.Courses;
using Domain.Activity;

namespace Application.Activities;

/// <summary>
/// Dodaje aktivnost postojećem predmetu. Katalog predmeta se ne menja; postojeći port
/// <see cref="ICourseReader"/> služi samo da se predmet pronađe po oznaci oblika
/// <c>OZNAKA-xxxxx</c> (vidi <see cref="EntitySlug"/>).
/// Neispravni podaci i otkazan zahtev vraćaju se kao status, bez izuzetaka.
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

        var creation = Activity.TryCreate(
            Guid.NewGuid(),
            matches[0].Id,
            command.Name,
            command.Description,
            command.StartDate,
            command.EndDate);

        if (creation.Activity is not { } activity)
        {
            return CreateActivityResult.InvalidRequest(
                creation.Error ?? "Nevažeći podaci aktivnosti.");
        }

        var outcome = await _repository.AddAsync(activity, cancellationToken);

        return outcome.Status switch
        {
            ActivityAddStatus.Added => CreateActivityResult.Created(ActivityListItem.From(activity)),
            ActivityAddStatus.DuplicateId => CreateActivityResult.Conflict(
                "Aktivnost sa istim identifikatorom već postoji."),
            ActivityAddStatus.Canceled => CreateActivityResult.Canceled(),
            _ => CreateActivityResult.InvalidRequest("Aktivnost nije ispravna.")
        };
    }
}
