namespace Domain.Attendance;

/// <summary>
/// Period važenja članstva studenta u grupi ili izvođenju (ST-032).
/// Početak je uključen, kraj nije: članstvo važi od <see cref="From"/>
/// do trenutka pre <see cref="To"/>. Bez kraja članstvo važi i dalje.
/// </summary>
/// <remarks>
/// Uključen početak i isključen kraj je privremena odluka dok asistent ne potvrdi
/// kako se tretira članstvo koje počinje ili prestaje tačno u vreme termina.
/// Vidi docs/adr/0001-st-032-podobnost-studenata.md.
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
