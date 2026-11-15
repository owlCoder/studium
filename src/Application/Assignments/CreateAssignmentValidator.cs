
namespace Application.Assignments;

public static class CreateAssignmentValidator
{
    public static void Validate(CreateAssignmentCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.CourseOfferingId == Guid.Empty)
        {
            throw new ArgumentException(
                "Izvodjenje predmeta je obavezno.",
                nameof(command.CourseOfferingId));
        }

        if (string.IsNullOrWhiteSpace(command.Title))
        {
            throw new ArgumentException(
                "Naziv zadatka je obavezan.",
                nameof(command.Title));
        }
    }
}
