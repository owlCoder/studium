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
    public string Description { get; private set; }

    /// <summary>Aktivan predmet sme da dobije nova izvođenja (ST-004).</summary>
    public bool IsActive { get; private set; }

    public Course(Guid id, string code, string name, string description = "", bool isActive = true)
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
        Description = description?.Trim() ?? string.Empty;
        IsActive = isActive;
    }

    public void Update(string code, string name, string description, ICourseOfferingLookup offeringLookup)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Oznaka predmeta je obavezna.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Naziv predmeta je obavezan.", nameof(name));

        string normalizedCode = code.Trim().ToUpperInvariant();

        if (Code != normalizedCode && offeringLookup.HasOfferings(Id))
        {
            throw new InvalidOperationException("Oznaka predmeta se ne može menjati jer je predmet povezan sa aktivnim izvođenjem.");
        }

        Code = normalizedCode;
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Ponovna aktivacija predmeta. Menja samo aktivnost: oznaka, naziv i opis ostaju
    /// netaknuti. Poziv nad već aktivnim predmetom ne menja ništa.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }
}
