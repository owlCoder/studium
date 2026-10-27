namespace Studium.Domain.Courses;

/// <summary>
/// Ilustrativni model kataloškog predmeta; nije kompletna funkcionalnost ST-003.
/// </summary>
public sealed class Course
{
    public Guid Id { get; }
    public string Code { get; }
    public string Name { get; }

    public Course(Guid id, string code, string name)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Identifikator mora biti zadat.", nameof(id));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Oznaka predmeta je obavezna.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Naziv predmeta je obavezan.", nameof(name));

        Id = id;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
    }
}
