using Application.StudentImport;

namespace Infrastructure.StudentImport;

/// <summary>Меморијска трансакција.</summary>
/// <remarks>
/// Угнежђени позив дели трансакцију; изузетак поништава њене измене.
/// Учесници не смеју бацити изузетак из методе <c>Commit</c>.
/// Дочерњи задаци морају завршити пре краја посла. Не обезбеђује изолацију базе.
/// </remarks>
public sealed class InMemoryUnitOfWork : IUnitOfWork
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private readonly InMemoryTransaction[] _participants;

    /// <summary>Учесници текуће трансакције, наслеђени и у дочерњим задацима.</summary>
    private static readonly AsyncLocal<TransactionScope?> Current = new();

    private sealed class TransactionScope(InMemoryTransaction[] participants)
    {
        public InMemoryTransaction[] Participants { get; } = participants;
        public int Closed;
        public int Failed;
    }

    public InMemoryUnitOfWork(
        InMemoryStudentAccountDirectory accounts,
        InMemoryFirstActivationService activations,
        InMemoryEnrollmentDirectory enrollments)
        : this(new InMemoryTransaction[] { accounts, activations, enrollments })
    {
    }

    public InMemoryUnitOfWork(params InMemoryTransaction[] participants)
    {
        ArgumentNullException.ThrowIfNull(participants);
        if (participants.Any(p => p is null) || participants.Distinct(ReferenceEqualityComparer.Instance).Count() != participants.Length)
            throw new ArgumentException("Ucesnici moraju biti razliciti i zadati.", nameof(participants));
        _participants = participants.ToArray();
    }

    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> work,
        CancellationToken cancellationToken = default)
    {
        if (work is null) throw new ArgumentNullException(nameof(work));

        cancellationToken.ThrowIfCancellationRequested();
        if (Current.Value is { } active)
        {
            if (Volatile.Read(ref active.Closed) != 0)
                throw new InvalidOperationException("Nasledjena transakcija je vec zavrsena.");
            try
            {
                if (_participants.Any(p => !active.Participants.Any(a => ReferenceEquals(a, p))))
                    throw new InvalidOperationException("Ugnezdena transakcija ne moze dodati ucesnike.");
                return await work(cancellationToken);
            }
            catch
            {
                Interlocked.Exchange(ref active.Failed, 1);
                throw;
            }
        }

        // Једна брава чува и различите инстанце које деле меморијске адаптере.
        await Gate.WaitAsync(cancellationToken);
        var begun = new List<InMemoryTransaction>();
        var scope = new TransactionScope(_participants);
        try
        {
            foreach (var participant in _participants)
            {
                participant.Begin();
                begun.Add(participant);
            }
            Current.Value = scope;
            var result = await work(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (Volatile.Read(ref scope.Failed) != 0)
                throw new InvalidOperationException("Ugnezdeni posao je pao; transakcija se ponistava.");
            foreach (var participant in _participants) participant.Commit();
            return result;
        }
        catch (Exception failure)
        {
            var failures = new List<Exception> { failure };
            foreach (var participant in begun.AsEnumerable().Reverse())
            {
                try { participant.Rollback(); }
                catch (Exception cleanup) { failures.Add(cleanup); }
            }
            if (failures.Count > 1)
                throw new AggregateException("Transakcija i njeno ponistavanje nisu uspeli.", failures);
            throw;
        }
        finally
        {
            Interlocked.Exchange(ref scope.Closed, 1);
            Current.Value = null;
            Gate.Release();
        }
    }
}

/// <summary>Део посла који уме да сачува и врати своје стање.</summary>
public interface InMemoryTransaction
{
    void Begin();

    /// <summary>Завршава посао без изузетка; после тога повратак није гарантован.</summary>
    void Commit();

    void Rollback();
}
