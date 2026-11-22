namespace Application.StudentImport;

/// <summary>Проверава право увоза при свакој операцији.</summary>
public sealed class StudentImportAuthorizer : IStudentImportAuthorizer
{
    private readonly ICurrentUser _currentUser;
    private readonly ITeachingAssignmentDirectory _assignments;

    public StudentImportAuthorizer(ICurrentUser currentUser, ITeachingAssignmentDirectory assignments)
    {
        _currentUser = currentUser;
        _assignments = assignments;
    }

    /// <summary>Увоз сме да покрене администратор или одговорни наставник извођења.</summary>
    public async Task EnsureCanImportAsync(Guid offeringId, CancellationToken cancellationToken = default)
    {
        // Празан идентификатор не представља пријављеног корисника.
        if (_currentUser.AccountId is null || _currentUser.AccountId == Guid.Empty)
            throw new UnauthorizedAccessException("Korisnik nije prijavljen.");

        if (offeringId == Guid.Empty)
            throw new ArgumentException("Izvodjenje predmeta mora biti zadato.", nameof(offeringId));

        // Назив улоге се пореди без разлике у величини слова.
        if (string.Equals(_currentUser.Role, AccountRoles.Administrator, StringComparison.OrdinalIgnoreCase))
            return;

        if (!string.Equals(_currentUser.Role, AccountRoles.Nastavnik, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException(
                "Samo administrator ili odgovorni nastavnik moze da uvozi studente.");
        }

        var responsible = await _assignments.IsResponsibleTeacherAsync(
            _currentUser.AccountId.Value, offeringId, cancellationToken);

        if (!responsible)
        {
            throw new UnauthorizedAccessException(
                "Nastavnik nije odgovorni nastavnik na ovom izvodjenju predmeta.");
        }
    }
}
