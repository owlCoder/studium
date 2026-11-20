
namespace Application.Assignments;

public sealed class CreateAssignmentHandler
{
    private readonly IAssignmentCreationAccess _access;
    private readonly IAssignmentDraftWriter _draftWriter;

    public CreateAssignmentHandler(
        IAssignmentCreationAccess access,
        IAssignmentDraftWriter draftWriter)
    {
        _access = access ??
            throw new ArgumentNullException(nameof(access));

        _draftWriter = draftWriter ??
            throw new ArgumentNullException(nameof(draftWriter));
    }

    public async Task<Guid> HandleAsync(
        CreateAssignmentCommand command,
        CancellationToken cancellationToken = default)
    {
        // Provera osnovnih ulaznih podataka.
        CreateAssignmentValidator.Validate(command);

        // Provera da li korisnik sme da kreira zadatak.
        bool allowed = await _access.CanCreateAsync(
            command.CourseOfferingId,
            cancellationToken);

        if (!allowed)
        {
            throw new UnauthorizedAccessException(
                "Korisnik nema pravo da kreira zadatak " +
                "za izabrano izvodjenje predmeta.");
        }

        // Generisanje identifikatora novog zadatka.
        Guid assignmentId = Guid.NewGuid();

        // Prosledjivanje podataka modulu za cuvanje nacrta.
        await _draftWriter.SaveAsync(
            assignmentId,
            command,
            cancellationToken);

        return assignmentId;
    }
}
