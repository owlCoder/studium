namespace Infrastructure.Access;

/// <summary>
/// Vrsta veze korisnika sa izvođenjem.
/// </summary>
public enum MembershipKind
{
    Student,
    Teacher,
    Assistant
}

/// <summary>
/// Jedan zapis o članstvu ili angažovanju na izvođenju (test podaci za KT1).
/// </summary>
public sealed record OfferingMembership(
    Guid UserId,
    Guid OfferingId,
    MembershipKind Kind,
    bool IsActive);
