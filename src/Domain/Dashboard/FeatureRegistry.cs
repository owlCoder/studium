namespace Domain.Dashboard;

public sealed class FeatureRegistry
{
    private readonly Dictionary<string, SupportedActivityType> _byCode;

    public FeatureRegistry(IEnumerable<SupportedActivityType> supportedTypes)
    {
        ArgumentNullException.ThrowIfNull(supportedTypes);

        _byCode = new Dictionary<string, SupportedActivityType>(StringComparer.Ordinal);
        foreach (var type in supportedTypes)
        {
            ArgumentNullException.ThrowIfNull(type);
            if (!_byCode.TryAdd(type.Code, type))
            {
                throw new ArgumentException(
                    $"Vrsta aktivnosti '{type.Code}' je navedena više puta.", nameof(supportedTypes));
            }
        }
    }

    public IReadOnlyCollection<SupportedActivityType> SupportedActivityTypes => _byCode.Values;

    public IReadOnlyCollection<FunctionalArea> AvailableAreas =>
        _byCode.Values.Select(type => type.Area).Distinct().OrderBy(area => area).ToArray();

    public bool IsActivitySupported(string? activityType)
    {
        var code = SupportedActivityType.Normalize(activityType);
        return code is not null && _byCode.ContainsKey(code);
    }

    public bool IsAreaAvailable(FunctionalArea area)
    {
        return _byCode.Values.Any(type => type.Area == area);
    }

    public string? TryBuildLink(string? activityType, Guid offeringId, Guid itemId)
    {
        var code = SupportedActivityType.Normalize(activityType);
        if (code is null || offeringId == Guid.Empty || itemId == Guid.Empty)
        {
            return null;
        }

        return _byCode.TryGetValue(code, out var type) ? type.BuildLink(offeringId, itemId) : null;
    }
}
