namespace Domain.Courses;

/// <summary>
/// Ilustrativni model kataloškog predmeta; nije kompletna funkcionalnost ST-003.
/// </summary>
public sealed class Course
{
    public Guid Id { get; }
    public string Code { get; }
    public string Name { get; }
    public string Description { get; set; }
    public bool IsActive { get; set; }

    public Course(Guid id, string code, string name, string description)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Identifikator mora biti zadat.", nameof(id));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Oznaka predmeta je obavezna.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Naziv predmeta je obavezan.", nameof(name));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Opis predmeta je obavezan.", nameof(description));

        Id = id;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Description = description.Trim();
        IsActive = true;
    }
}
