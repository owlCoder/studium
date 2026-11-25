namespace Application.Home;


public sealed record HomeOverviewDto(
    IReadOnlyList<HomeOfferingDto> Teaching,
    IReadOnlyList<HomeOfferingDto> Enrolled);
