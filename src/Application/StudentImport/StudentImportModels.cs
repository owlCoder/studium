namespace Application.StudentImport;

/// <summary>Улоге налога које користи увоз.</summary>
public enum AccountRole
{
    Administrator,
    Nastavnik,
    Asistent,
    Student,
}

/// <summary>Имена улога као текст, за поређење са улогом коју даје спољни слој.</summary>
public static class AccountRoles
{
    public const string Administrator = "Administrator";
    public const string Nastavnik = "Nastavnik";
    public const string Asistent = "Asistent";
    public const string Student = "Student";

    public static bool TryParse(string? value, out AccountRole role)
    {
        if (string.Equals(value, Administrator, StringComparison.OrdinalIgnoreCase))
        {
            role = AccountRole.Administrator;
            return true;
        }

        if (string.Equals(value, Nastavnik, StringComparison.OrdinalIgnoreCase))
        {
            role = AccountRole.Nastavnik;
            return true;
        }

        if (string.Equals(value, Asistent, StringComparison.OrdinalIgnoreCase))
        {
            role = AccountRole.Asistent;
            return true;
        }

        if (string.Equals(value, Student, StringComparison.OrdinalIgnoreCase))
        {
            role = AccountRole.Student;
            return true;
        }

        role = default;
        return false;
    }
}

/// <summary>Стање корисничког налога.</summary>
public enum AccountState
{
    Aktivan,
    Deaktiviran,
}

/// <summary>Стање извођења предмета.</summary>
public enum OfferingState
{
    UPripremi,
    Aktivno,
    Zavrseno,
}

/// <summary>Подаци извођења; одговорни наставници се читају преко засебног порта.</summary>
public sealed record OfferingSnapshot(
    Guid OfferingId,
    Guid CourseId,
    Guid AcademicYearId,
    OfferingState State,
    bool IsArchived);

/// <summary>Одговорност на извођењу.</summary>
public enum TeachingResponsibility
{
    OdgovorniNastavnik,
    Nastavnik,
    Asistent,
}

/// <summary>Стање реда увоза.</summary>
public enum RowStatus
{
    Ispravan,
    Duplikat,
    Greska
}

/// <summary>Резултат обраде једног реда. Разлог је попуњен кад ред није исправан.</summary>
public sealed record ImportRowResult(
    int RowNumber,
    string Username,
    RowStatus Status,
    string Reason);

/// <summary>Редови предлога са стањем, разлогом и упозорењима за датотеку.</summary>
/// <remarks>Бројачи се изводе из редова, а број реда одговара месту у датотеци.</remarks>
public sealed record ImportPreview(
    Guid OfferingId,
    IReadOnlyList<ImportRowResult> Rows,
    IReadOnlyList<string> Warnings)
{
    /// <summary>Укупан број обрађених редова (без празних).</summary>
    public int TotalCount => Rows.Count;

    public int CorrectCount => Rows.Count(r => r.Status == RowStatus.Ispravan);

    public int DuplicateCount => Rows.Count(r => r.Status == RowStatus.Duplikat);

    public int ErrorCount => Rows.Count(r => r.Status == RowStatus.Greska);
}

/// <summary>Резултат потврде. Бројеви се изводе из стварне обраде, не из предлога.</summary>
/// <remarks>Број уписаних чланстава укључује поново активирана чланства.</remarks>
public sealed record ImportConfirmationResult(
    int CreatedAccountCount,
    int CreatedMembershipCount,
    int DuplicateCount,
    int ErrorCount,
    IReadOnlyList<string> Warnings);
