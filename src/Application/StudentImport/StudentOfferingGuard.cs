namespace Application.StudentImport;

/// <summary>Уговор за проверу постојања извођења предмета.</summary>
public interface IStudentOfferingGuard
{
    Task<OfferingSnapshot> EnsureExistsAsync(
        Guid offeringId,
        CancellationToken cancellationToken = default);
}

/// <summary>Проверава постојање извођења за предлог и потврду.</summary>
/// <remarks>Проверава само постојање извођења, без додатне политике уписа.</remarks>
public sealed class StudentOfferingGuard : IStudentOfferingGuard
{
    private readonly IOfferingDirectory _offerings;

    public StudentOfferingGuard(IOfferingDirectory offerings) => _offerings = offerings;

    /// <exception cref="ArgumentException">Извођење не постоји.</exception>
    public async Task<OfferingSnapshot> EnsureExistsAsync(
        Guid offeringId,
        CancellationToken cancellationToken = default)
        => await _offerings.FindAsync(offeringId, cancellationToken)
            ?? throw new ArgumentException(
                "Izvodjenje predmeta ne postoji.", nameof(offeringId));
}
