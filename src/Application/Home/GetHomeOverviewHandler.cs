using Application.Activities;
using Application.Engagements;
using Domain.Dashboard;

namespace Application.Home;


public sealed class GetHomeOverviewHandler
{
    private readonly ListMyOfferingsHandler _myOfferings;
    private readonly IOpenActivityCounter _openActivities;
    private readonly FeatureRegistry _registry;
    private readonly IOpenTaskLinkReader _openTasks;

    public GetHomeOverviewHandler(
        ListMyOfferingsHandler myOfferings,
        IOpenActivityCounter openActivities,
        FeatureRegistry registry)
        : this(myOfferings, openActivities, registry, NoOpenTaskLinks.Instance)
    {
    }

    public GetHomeOverviewHandler(
        ListMyOfferingsHandler myOfferings,
        IOpenActivityCounter openActivities,
        FeatureRegistry registry,
        IOpenTaskLinkReader openTasks)
    {
        _myOfferings = myOfferings ?? throw new ArgumentNullException(nameof(myOfferings));
        _openActivities = openActivities ?? throw new ArgumentNullException(nameof(openActivities));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _openTasks = openTasks ?? throw new ArgumentNullException(nameof(openTasks));
    }

    public async Task<HomeOverviewDto> HandleAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("Korisnik mora biti zadat.", nameof(userId));
        }


        var offerings = await _myOfferings.HandleAsync(userId, cancellationToken);

        var teaching = offerings.Teaching
            .Select(row => new HomeOfferingDto(
                row.OfferingId,
                row.CourseName,
                row.AcademicYear,
                row.RoleName,
                OpenActivities: null,
                LinksFor(row.OfferingId)))
            .ToArray();


        var enrolled = new List<HomeOfferingDto>(offerings.Enrolled.Count);
        foreach (var row in offerings.Enrolled)
        {
            var open = await _openActivities.CountOpenAsync(userId, row.OfferingId, cancellationToken);
            var openTasks = await _openTasks.ListForStudentAsync(userId, row.OfferingId, cancellationToken);

            enrolled.Add(new HomeOfferingDto(
                row.OfferingId,
                row.CourseName,
                row.AcademicYear,
                Role: null,
                OpenActivities: open,
                LinksFor(row.OfferingId))
            {
                OpenTasks = openTasks
            });
        }

        return new HomeOverviewDto(teaching, enrolled);
    }


    private IReadOnlyList<HomeLinkDto> LinksFor(Guid offeringId)
    {
        var links = new List<HomeLinkDto>
        {
            new("izvodjenje", $"/offerings/{offeringId}")
        };

        foreach (var area in _registry.AvailableAreas.Where(_registry.IsAreaAvailable))
        {
            var segment = area.ToString().ToLowerInvariant();
            links.Add(new HomeLinkDto(segment, $"/offerings/{offeringId}/{segment}"));
        }

        return links;
    }

    private sealed class NoOpenTaskLinks : IOpenTaskLinkReader
    {
        public static readonly NoOpenTaskLinks Instance = new();

        public Task<IReadOnlyList<OpenTaskLinkDto>> ListForStudentAsync(
            Guid studentId,
            Guid offeringId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OpenTaskLinkDto>>(Array.Empty<OpenTaskLinkDto>());
    }
}
