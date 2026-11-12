using System.Text;
using Application.Attendance;
using Application.TeachingSessions;
using Domain.Attendance;
using Domain.TeachingSessions;
using Infrastructure.Attendance;
using Infrastructure.TeachingSessions;

Console.OutputEncoding = Encoding.UTF8;

var offeringId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
var groupId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
var anaId = Guid.Parse("11111111-1111-1111-1111-111111111111");
var markoId = Guid.Parse("22222222-2222-2222-2222-222222222222");
var actorId = Guid.Parse("99999999-9999-9999-9999-999999999999");

var names = new Dictionary<Guid, string>
{
    [anaId] = "Ana",
    [markoId] = "Marko",
};

var offerings = new InMemoryCourseOfferingReader()
    .AddOffering(offeringId)
    .AddGroup(offeringId, groupId);
var eligibility = new InMemoryEligibilityProvider()
    .Add(offeringId, groupId, anaId)
    .Add(offeringId, groupId, markoId);
var attendance = new InMemoryAttendanceRepository();

var createSession = new CreateSessionHandler(offerings);
var recordAttendance = new RecordAttendanceHandler(eligibility, attendance);
var listEligible = new ListEligibleStudentsHandler(eligibility, attendance);

Console.WriteLine("STUDIUM | KT1 scenario (ST-031 + ST-032)");
Console.WriteLine();

// 1. Odbijen termin koji se završava pre početka.
Console.WriteLine("1) Vežbe 10:00 do 09:30");
try
{
    await createSession.HandleAsync(new CreateSessionCommand(
        "Vežbe", offeringId, groupId,
        new DateTime(2026, 11, 7, 10, 0, 0),
        new DateTime(2026, 11, 7, 9, 30, 0),
        "Učionica 101"));
}
catch (ArgumentException ex)
{
    Console.WriteLine($"   Odbijeno: {ex.Message}");
}
Console.WriteLine();

// 2. Validan termin nastaje u stanju Planiran.
var session = await createSession.HandleAsync(new CreateSessionCommand(
    "Vežbe", offeringId, groupId,
    new DateTime(2026, 11, 7, 10, 0, 0),
    new DateTime(2026, 11, 7, 11, 30, 0),
    "Učionica 101"));
Console.WriteLine($"2) Termin kreiran: {session.State}");
Console.WriteLine();

// 3. Termin se označava kao Održan.
session.MarkHeld();
Console.WriteLine($"3) Termin označen: {session.State}");
Console.WriteLine();

// 4. Podobni studenti, pre unosa prisustva.
Console.WriteLine("4) Podobni studenti (pre unosa):");
foreach (var student in await listEligible.HandleAsync(session))
{
    var label = names.GetValueOrDefault(student.StudentId, student.StudentId.ToString("N"));
    Console.WriteLine($"   {label}: {student.State}");
}
Console.WriteLine();

// 5. Unos prisustva samo za Anu.
await recordAttendance.HandleAsync(session, anaId, AttendanceState.Prisutan, actorId);
Console.WriteLine("5) Uneto prisustvo: Ana -> Prisutan");
Console.WriteLine();

// 6. Rezultat: Marko ostaje Neevidentirano, ne Odsutan.
Console.WriteLine("6) Rezultat:");
foreach (var student in await listEligible.HandleAsync(session))
{
    var label = names.GetValueOrDefault(student.StudentId, student.StudentId.ToString("N"));
    Console.WriteLine($"   {label}: {student.State}");
}
Console.WriteLine();

// 7. Otkazan termin odbija novi unos prisustva.
var cancelled = await createSession.HandleAsync(new CreateSessionCommand(
    "Vežbe", offeringId, groupId,
    new DateTime(2026, 11, 7, 10, 0, 0),
    new DateTime(2026, 11, 7, 11, 30, 0),
    "Učionica 101"));
cancelled.Cancel();
Console.WriteLine("7) Otkazan termin odbija novi unos:");
try
{
    await recordAttendance.HandleAsync(cancelled, markoId, AttendanceState.Prisutan, actorId);
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"   Odbijeno: {ex.Message}");
}
