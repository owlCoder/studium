using Application.StudentImport;

namespace Infrastructure.StudentImport;

// Меморијски адаптери за тестове; не чувају податке трајно.

/// <summary>Меморијски адаптер за <see cref="IAccountDirectory"/>.</summary>
/// <remarks>Провера и упис налога користе исто закључавање, да паралелни позиви не створе дупликат.</remarks>
public sealed class InMemoryStudentAccountDirectory : IAccountDirectory, InMemoryTransaction
{
    private readonly object _gate = new();

    private readonly Dictionary<string, AccountSnapshot> _accounts =
        new(UsernameComparison.Comparer);

    /// <summary>Претходне вредности налога за враћање измена текућег посла.</summary>
    private readonly AsyncLocal<Dictionary<string, AccountSnapshot?>?> _undo = new();

    private int _lookupCount;

    public void Begin()
    {
        lock (_gate)
        {
            _undo.Value = new Dictionary<string, AccountSnapshot?>(UsernameComparison.Comparer);
        }
    }

    public void Commit()
    {
        lock (_gate)
        {
            _undo.Value = null;
        }
    }

    public void Rollback()
    {
        lock (_gate)
        {
            var undo = _undo.Value;
            _undo.Value = null;
            if (undo is null) return;

            foreach (var (key, before) in undo)
            {
                if (before is null) _accounts.Remove(key);
                else _accounts[key] = before;
            }
        }
    }

    /// <summary>Број упита за проверу кеширања у тестовима.</summary>
    public int LookupCount
    {
        get
        {
            lock (_gate) return _lookupCount;
        }
    }

    public void Seed(AccountSnapshot account)
    {
        ArgumentNullException.ThrowIfNull(account);
        if (account.AccountId == Guid.Empty)
            throw new ArgumentException("Identifikator naloga mora biti zadat.", nameof(account));

        lock (_gate)
        {
            var key = UsernameComparison.Key(account.Username);
            RememberUndo(key, _accounts.GetValueOrDefault(key));
            _accounts[key] = account;
        }
    }

    public void Seed(
        string username,
        Guid accountId,
        AccountRole role = AccountRole.Student,
        AccountState state = AccountState.Aktivan,
        bool isActivated = true,
        string firstName = "Test",
        string lastName = "Student",
        string? email = null,
        bool isEmailConfirmed = false)
        => Seed(new AccountSnapshot(
            AccountId: accountId,
            FirstName: firstName,
            LastName: lastName,
            Username: UsernameComparison.Canonical(username),
            Role: role,
            State: state,
            IsActivated: isActivated,
            Email: email,
            IsEmailConfirmed: isEmailConfirmed));

    public Task<AccountSnapshot?> FindByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            _lookupCount++;

            if (string.IsNullOrWhiteSpace(username))
                return Task.FromResult<AccountSnapshot?>(null);

            return Task.FromResult(
                _accounts.TryGetValue(UsernameComparison.Key(username), out var account)
                    ? account
                    : null);
        }
    }

    public Task<AccountRegistrationResult> TryAddAsync(
        NewUserAccount data,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateNewAccount(data);

        lock (_gate)
        {
            var key = UsernameComparison.Key(data.Username);
            if (_accounts.TryGetValue(key, out var existing))
            {
                return Task.FromResult(new AccountRegistrationResult(existing, Created: false));
            }

            var created = new AccountSnapshot(
                AccountId: Guid.NewGuid(),
                FirstName: data.FirstName.Trim(),
                LastName: data.LastName.Trim(),
                Username: data.Username.Trim(),
                Role: data.Role,
                State: data.State,
                IsActivated: data.IsActivated,
                Email: string.IsNullOrWhiteSpace(data.Email) ? null : data.Email.Trim(),
                IsEmailConfirmed: data.IsEmailConfirmed);

            // Поништавање уклања налог који није постојао пре ове трансакције.
            RememberUndo(key, before: null);
            _accounts[key] = created;
            return Task.FromResult(new AccountRegistrationResult(created, Created: true));
        }
    }

    /// <summary>Памти само вредност пре прве измене, потребну за враћање стања.</summary>
    private void RememberUndo(string key, AccountSnapshot? before)
    {
        var undo = _undo.Value;
        if (undo is null) return;

        if (!undo.ContainsKey(key)) undo[key] = before;
    }

    /// <summary>Упис који пријављује грешку ако корисничко име већ постоји.</summary>
    public async Task<Guid> AddAsync(
        NewUserAccount data,
        CancellationToken cancellationToken = default)
    {
        var result = await TryAddAsync(data, cancellationToken);
        if (!result.Created)
        {
            throw new InvalidOperationException(
                "Nalog sa tim korisnickim imenom vec postoji (ST-001, jedinstvenost).");
        }

        return result.Account.AccountId;
    }

    private static void ValidateNewAccount(NewUserAccount data)
    {
        ArgumentNullException.ThrowIfNull(data);

        // Име, презиме и јединствено корисничко име су обавезни подаци налога.
        if (string.IsNullOrWhiteSpace(data.FirstName) || string.IsNullOrWhiteSpace(data.LastName))
            throw new ArgumentException("Ime i prezime su obavezni.", nameof(data));

        if (string.IsNullOrWhiteSpace(data.Username))
            throw new ArgumentException("Korisnicko ime je obavezno.", nameof(data));

        // Нови налог није активиран док корисник не заврши прву активацију.
        if (data.IsActivated)
            throw new ArgumentException("Novi nalog ne moze biti unapred aktiviran (ST-001).", nameof(data));

        if (data.State != AccountState.Aktivan)
            throw new ArgumentException("Novi nalog mora nastati u aktivnom stanju (ST-001).", nameof(data));
    }
}

/// <summary>Меморијски адаптер за <see cref="IFirstActivationService"/>.</summary>
/// <remarks>Бележи захтеве за прву активацију и подржава поништавање текућег посла.</remarks>
public sealed class InMemoryFirstActivationService : IFirstActivationService, InMemoryTransaction
{
    /// <summary>Идентитет објекта омогућава брисање баш овог захтева при поништавању.</summary>
    private sealed class ActivationRecord
    {
        public ActivationRecord(Guid accountId) => AccountId = accountId;

        public Guid AccountId { get; }
    }

    private readonly object _gate = new();
    private readonly List<ActivationRecord> _activationRequested = new();

    /// <summary>Захтеви које треба уклонити ако текући посао не успе.</summary>
    private readonly AsyncLocal<List<ActivationRecord>?> _undo = new();

    public IReadOnlyList<Guid> ActivationRequested
    {
        get
        {
            lock (_gate) return _activationRequested.Select(r => r.AccountId).ToArray();
        }
    }

    public void Begin()
    {
        lock (_gate)
        {
            _undo.Value = new List<ActivationRecord>();
        }
    }

    public void Commit()
    {
        lock (_gate)
        {
            _undo.Value = null;
        }
    }

    public void Rollback()
    {
        lock (_gate)
        {
            var undo = _undo.Value;
            _undo.Value = null;
            if (undo is null) return;

            foreach (var record in undo) _activationRequested.Remove(record);
        }
    }

    public Task StartAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (accountId == Guid.Empty)
            throw new ArgumentException("Identifikator naloga mora biti zadat.", nameof(accountId));

        lock (_gate)
        {
            var record = new ActivationRecord(accountId);
            _undo.Value?.Add(record);
            _activationRequested.Add(record);
        }

        return Task.CompletedTask;
    }
}

/// <summary>Меморијски адаптер за <see cref="IEnrollmentDirectory"/>.</summary>
/// <remarks>Пар студента и извођења одређује једно чланство. Провера и упис су под истим закључавањем.</remarks>
public sealed class InMemoryEnrollmentDirectory : IEnrollmentDirectory, InMemoryTransaction
{
    private readonly object _gate = new();
    private readonly Dictionary<(Guid AccountId, Guid OfferingId), EnrollmentSnapshot> _enrollments = new();
    private readonly AsyncLocal<Dictionary<(Guid, Guid), EnrollmentSnapshot?>?> _undo = new();

    public int Count { get { lock (_gate) return _enrollments.Count; } }

    public IReadOnlyList<EnrollmentSnapshot> All
    {
        get { lock (_gate) return _enrollments.Values.ToArray(); }
    }

    public IReadOnlyCollection<DateTimeOffset> EnrolledAtValues
    {
        get { lock (_gate) return _enrollments.Values.Select(e => e.EnrolledAt).ToArray(); }
    }

    public void Begin()
    {
        lock (_gate) _undo.Value = new();
    }

    public void Commit()
    {
        lock (_gate) _undo.Value = null;
    }

    public void Rollback()
    {
        lock (_gate)
        {
            var undo = _undo.Value;
            _undo.Value = null;
            if (undo is null) return;
            foreach (var (pair, before) in undo)
            {
                if (before is null) _enrollments.Remove(pair);
                else _enrollments[pair] = before;
            }
        }
    }

    /// <summary>Поставља једно чланство за тест.</summary>
    public void Seed(Guid accountId, Guid offeringId,
        EnrollmentState state = EnrollmentState.Aktivno, DateTimeOffset? enrolledAt = null)
    {
        if (accountId == Guid.Empty || offeringId == Guid.Empty)
            throw new ArgumentException("Identifikatori studenta i izvodjenja moraju biti zadati.");
        lock (_gate)
        {
            var pair = (accountId, offeringId);
            if (_enrollments.ContainsKey(pair)) return;
            Remember(pair);
            _enrollments[pair] = new EnrollmentSnapshot(Guid.NewGuid(), accountId, offeringId,
                enrolledAt ?? DateTimeOffset.UnixEpoch, state);
        }
    }

    public Task<EnrollmentSnapshot?> FindActiveAsync(Guid accountId, Guid offeringId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            var found = _enrollments.GetValueOrDefault((accountId, offeringId));
            return Task.FromResult(found?.State == EnrollmentState.Aktivno ? found : null);
        }
    }

    public Task<bool> TryAddActiveAsync(NewEnrollment enrollment,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(enrollment);
        if (enrollment.AccountId == Guid.Empty || enrollment.OfferingId == Guid.Empty)
            throw new ArgumentException("Identifikatori studenta i izvodjenja moraju biti zadati.", nameof(enrollment));
        if (enrollment.State != EnrollmentState.Aktivno)
            throw new ArgumentException("Upis clanstva mora biti u aktivnom stanju (ST-006).", nameof(enrollment));
        lock (_gate)
        {
            var pair = (enrollment.AccountId, enrollment.OfferingId);
            var before = _enrollments.GetValueOrDefault(pair);
            if (before?.State == EnrollmentState.Aktivno) return Task.FromResult(false);
            Remember(pair);
            // Исто чланство задржава идентитет и првобитни датум уписа.
            _enrollments[pair] = before is null
                ? new EnrollmentSnapshot(Guid.NewGuid(), enrollment.AccountId, enrollment.OfferingId,
                    enrollment.EnrolledAt, EnrollmentState.Aktivno)
                : before with { State = EnrollmentState.Aktivno };
            return Task.FromResult(true);
        }
    }

    private void Remember((Guid, Guid) pair)
    {
        var undo = _undo.Value;
        if (undo is not null && !undo.ContainsKey(pair))
            undo[pair] = _enrollments.GetValueOrDefault(pair);
    }
}

/// <summary>Меморијски адаптер за <see cref="IOfferingDirectory"/>.</summary>
/// <remarks>Чува извођења без додатних правила о стању и упису.</remarks>
public sealed class InMemoryOfferingDirectory : IOfferingDirectory
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, OfferingSnapshot> _offerings = new();

    public void Seed(OfferingSnapshot offering)
    {
        ArgumentNullException.ThrowIfNull(offering);
        if (offering.OfferingId == Guid.Empty)
            throw new ArgumentException("Identifikator izvodjenja mora biti zadat.", nameof(offering));

        lock (_gate)
        {
            _offerings[offering.OfferingId] = offering;
        }
    }

    public Task<OfferingSnapshot?> FindAsync(Guid offeringId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            return Task.FromResult(_offerings.GetValueOrDefault(offeringId));
        }
    }
}

/// <summary>Меморијски адаптер за <see cref="ITeachingAssignmentDirectory"/>.</summary>
/// <remarks>Меморијска евиденција наставничке одговорности за тестове.</remarks>
public sealed class InMemoryTeachingAssignmentDirectory : ITeachingAssignmentDirectory
{
    private readonly object _gate = new();

    private readonly Dictionary<(Guid StaffAccountId, Guid OfferingId), TeachingResponsibility> _assignments = new();

    public void Seed(Guid staffAccountId, Guid offeringId, TeachingResponsibility responsibility)
    {
        if (staffAccountId == Guid.Empty)
            throw new ArgumentException("Identifikator nastavnika mora biti zadat.", nameof(staffAccountId));

        if (offeringId == Guid.Empty)
            throw new ArgumentException("Identifikator izvodjenja mora biti zadat.", nameof(offeringId));

        lock (_gate)
        {
            _assignments[(staffAccountId, offeringId)] = responsibility;
        }
    }

    public Task<bool> IsResponsibleTeacherAsync(
        Guid staffAccountId,
        Guid offeringId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            return Task.FromResult(
                _assignments.TryGetValue((staffAccountId, offeringId), out var responsibility)
                && responsibility == TeachingResponsibility.OdgovorniNastavnik);
        }
    }
}

/// <summary>Тестна замена за тренутног корисника.</summary>
public sealed class SimpleCurrentUser : ICurrentUser
{
    public SimpleCurrentUser(Guid? accountId, string? role)
    {
        AccountId = accountId;
        Role = role;
    }

    public SimpleCurrentUser(Guid? accountId, AccountRole role)
        : this(accountId, role.ToString())
    {
    }

    public Guid? AccountId { get; }

    public string? Role { get; }
}

/// <summary>Фиксно време за поновљиве тестове.</summary>
public sealed class FixedSystemClock : ISystemClock
{
    public FixedSystemClock(DateTimeOffset utcNow) => UtcNow = utcNow;

    public FixedSystemClock(DateTime utcNow)
        : this(new DateTimeOffset(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc)))
    {
    }

    public DateTimeOffset UtcNow { get; }
}
