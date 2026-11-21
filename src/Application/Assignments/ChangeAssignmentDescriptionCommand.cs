namespace Application.Assignments;

/// <summary>
/// Podaci koje nastavnik šalje kada menja opis zadatka (ST-011, Izmena zadatka).
/// Nastavnik se ne šalje u komandi, nego se njegovo pravo proverava preko IAssignmentEditAccess.
/// </summary>
public class ChangeAssignmentDescriptionCommand
{
    public Guid AssignmentId { get; }
    public string? Description { get; }

    public ChangeAssignmentDescriptionCommand(Guid assignmentId, string? description)
    {
        AssignmentId = assignmentId;
        Description = description;
    }
}
