
namespace Domain.Courses;

using System;

/// <summary>
/// Ilustrativni model kataloškog predmeta; nije kompletna funkcionalnost ST-003.
/// </summary>
public sealed class Course
{
    public Guid Id { get; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string Description { get; set; }
    public bool IsActive { get; set; }

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
        Description = string.Empty;
        IsActive = true;
    }

    public Course(Guid id, string code, string name, string description)
        : this(id, code, name)
    {
        Description = description?.Trim() ?? string.Empty;
    }

    public void Update(string code, string name, string description, ICourseOfferingLookup offeringLookup)
    {
        ArgumentNullException.ThrowIfNull(offeringLookup);

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Oznaka predmeta je obavezna.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Naziv predmeta je obavezan.", nameof(name));

        string normalizedCode = code.Trim().ToUpperInvariant();

        if (Code != normalizedCode && offeringLookup.HasActiveOfferings(Id))
        {
            throw new InvalidOperationException("Oznaka predmeta se ne može menjati jer je predmet povezan sa aktivnim izvođenjem.");
        }

        Code = normalizedCode;
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
    }
}
