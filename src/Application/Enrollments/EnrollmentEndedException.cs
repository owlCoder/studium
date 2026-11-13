namespace Application.Enrollments;

/// <summary>
/// Baca se kada student pokuša da preda rad, a članstvo na predmetu mu je prekinuto
/// ili nikada nije ni bilo aktivno.
/// </summary>
public sealed class EnrollmentEndedException : Exception
{
    public EnrollmentEndedException(string message) : base(message) { }
}
