using Domain.Activity;

namespace Application.Activities;

/// <summary>
/// Dodaje aktivnost u kontekst koji razrešava <see cref="IActivityContextReader"/>.
/// Katalog predmeta se ne menja, a oznaka iz URL-a se ne tumači ovde: dokle god
/// čitač vraća <see cref="ActivityContext"/>, slučaj upotrebe ne zna da li je
/// kontekst kataloški predmet ili izvođenje predmeta.
/// Neispravni podaci i otkazan zahtev vraćaju se kao status, bez izuzetaka.
/// </summary>
public sealed class CreateActivityHandler
{
    private readonly IActivityRepository _repository;
    private readonly IActivityContextReader _contextReader;

    public CreateActivityHandler(
        IActivityRepository repository,
        IActivityContextReader contextReader)
    {
        _repository = repository;
        _contextReader = contextReader;
    }

    public async Task<CreateActivityResult> HandleAsync(
        CreateActivityCommand command,
        CancellationToken cancellationToken = default)
    {
        var lookup = await _contextReader.ResolveAsync(command.ContextSlug, cancellationToken);

        if (!lookup.IsSuccess)
        {
            return lookup.Status switch
            {
                ActivityContextStatus.NotFound =>
                    CreateActivityResult.ContextNotFound(command.ContextSlug),
                ActivityContextStatus.Canceled =>
                    CreateActivityResult.Canceled(),
                _ => CreateActivityResult.InvalidRequest(
                    lookup.Error ?? $"Oznaka konteksta \"{command.ContextSlug}\" nije jedinstvena.")
            };
        }

        var creation = Activity.TryCreate(
            Guid.NewGuid(),
            lookup.Context!.Id,
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
