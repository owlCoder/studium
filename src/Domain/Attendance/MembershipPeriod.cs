namespace Domain.Attendance;

/// <summary>
/// Period važenja članstva studenta u grupi ili izvođenju (ST-032).
/// Početak je uključen, kraj nije: članstvo važi od <see cref="From"/>
/// do trenutka pre <see cref="To"/>. Bez kraja članstvo važi i dalje.
/// </summary>
/// <remarks>
/// Asistent je potvrdio: student prebačen iz jedne grupe u drugu odmah se računa
/// u novoj grupi. Zato staro članstvo prestaje, a novo počinje u istom trenutku,
/// bez preklapanja. Vidi docs/adr/0005-st-032-podobnost-studenata.md.
/// </remarks>
public sealed class MembershipPeriod
{
    public DateTime From { get; }

    public DateTime? To { get; }

    public MembershipPeriod(DateTime from, DateTime? to = null)
    {
        if (to is DateTime end && end <= from)
            throw new ArgumentException(
                "Kraj članstva mora biti posle početka.", nameof(to));

        From = from;
        To = to;
    }

    /// <summary>
    /// Članstvo bez početnog ograničenja, koje važi i dalje.
    /// </summary>
    public static MembershipPeriod Always { get; } = new(DateTime.MinValue);

    public bool Includes(DateTime moment) =>
        moment >= From && (To is null || moment < To);
}
