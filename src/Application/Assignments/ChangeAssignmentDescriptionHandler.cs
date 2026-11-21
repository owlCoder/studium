using Domain.Assignments;

namespace Application.Assignments;

/// <summary>
/// Obrađuje izmenu opisa zadatka (ST-011, Izmena zadatka).
/// Opis može da menja samo nastavno osoblje angažovano na izvođenju predmeta,
/// i samo dok je zadatak u stanju Nacrt ili Objavljen.
/// Ako se izmena odbije, zadatak ostaje isti i ništa se ne čuva.
/// </summary>
public class ChangeAssignmentDescriptionHandler
{
    private readonly IAssignmentRepository _repository;
    private readonly IAssignmentEditAccess _access;

    public ChangeAssignmentDescriptionHandler(IAssignmentRepository repository, IAssignmentEditAccess access)
    {
        _repository = repository;
        _access = access;
    }

    public async Task<ChangeAssignmentDescriptionResult> HandleAsync(ChangeAssignmentDescriptionCommand command)
    {
        // 1. Pročitamo zadatak čiji opis se menja.
        Assignment? assignment = await _repository.FindAsync(command.AssignmentId);

        // 2. Ako zadatak ne postoji, odbijamo izmenu.
        if (assignment == null)
        {
            return ChangeAssignmentDescriptionResult.Fail("Zadatak ne postoji.");
        }

        // 3. Samo nastavno osoblje angažovano na tom izvođenju predmeta može da menja opis.
        bool canEdit = await _access.CanEditAsync(assignment.CourseOfferingId);
        if (!canEdit)
        {
            return ChangeAssignmentDescriptionResult.Fail("Nemate pravo da menjate zadatak na ovom izvođenju predmeta.");
        }

        // 4. Menjamo opis. Zadatak sam proverava da li je u stanju Nacrt ili Objavljen.
        string? error = assignment.ChangeDescription(command.Description);
        if (error != null)
        {
            return ChangeAssignmentDescriptionResult.Fail(error);
        }

        // 5. Izmena je uspela, čuvamo zadatak.
        await _repository.SaveAsync(assignment);

        return ChangeAssignmentDescriptionResult.Ok();
    }
}
