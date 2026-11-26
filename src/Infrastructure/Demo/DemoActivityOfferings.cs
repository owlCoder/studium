using Domain.Offerings;

namespace Infrastructure.Demo;

/// <summary>
/// Demo izvođenja predmeta za kontekst aktivnosti. Koriste iste identifikatore kao demo
/// podaci ST-007, ali su izdvojeni u zaseban fajl da se ne dira ST-004 rad; kada ST-004
/// uvede svoje skladište, ovaj spisak se zamenjuje njihovim čitaocem.
/// </summary>
public static class DemoActivityOfferings
{
    public static readonly Guid ErsOfferingId = Guid.Parse("33333333-0000-0000-0000-000000000001");
    public static readonly Guid OibOfferingId = Guid.Parse("33333333-0000-0000-0000-000000000002");
    public static readonly Guid ErsPreviousYearOfferingId = Guid.Parse("33333333-0000-0000-0000-000000000003");

    public static IReadOnlyList<Offering> Create() => new[]
    {
        new Offering(ErsOfferingId, DemoData.ErsId, "2026/27"),
        new Offering(OibOfferingId, DemoData.OibId, "2026/27"),
        new Offering(ErsPreviousYearOfferingId, DemoData.ErsId, "2025/26")
    };
}
