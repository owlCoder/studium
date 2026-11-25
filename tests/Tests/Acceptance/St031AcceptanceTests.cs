using NUnit.Framework;
using Application.Attendance;
using Application.TeachingSessions;
using Domain.Attendance;
using Domain.TeachingSessions;
using Infrastructure.Attendance;
using Infrastructure.TeachingSessions;

namespace Tests.Acceptance;

/// <summary>
/// Testovi prihvatanja za ST-031, po primeru iz specifikacije:
/// "Vežbe za V1 traju od 10:00 do 11:30. Završetak u 09:30 se odbija.
/// Otkazivanjem termin ostaje u istoriji, ali se više ne prikazuje kao aktivna
/// obaveza i ne dozvoljava nov unos prisustva."
/// </summary>
[TestFixture]
public sealed class St031AcceptanceTests
{

}
