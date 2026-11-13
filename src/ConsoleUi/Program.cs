using System.Text;
using Application.Courses;
using Application.Materials;
using ConsoleUi.Materials;
using Domain.Materials;
using Infrastructure.Demo;
using Infrastructure.Materials;

Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine("STUDIUM | Demonstracija čitanja kataloga (nije ST-003)");

var handler = new ListCoursesHandler(DemoData.CreateCourseReader());
var courses = await handler.HandleAsync();

foreach (var course in courses)
    Console.WriteLine($"{course.Code} — {course.Name}");

// ST-009, US-02: forma za izmenu naziva i opisa nad sintetičkim materijalom u memoriji.
// Podaci se ne čuvaju trajno; provera nastavnika izvođenja (ST-005) nije uključena.
Console.WriteLine();
Console.WriteLine("STUDIUM | ST-009: kreiranje i izmena materijala (sintetički podaci u memoriji)");

var materials = new InMemoryMaterialRepository();
var demoMaterial = new Material(Guid.NewGuid(), Guid.NewGuid(), "Predavanje 1", "Uvod u predmet");
await materials.AddAsync(demoMaterial);

// ST-009: forma za unos naziva i opisa novog materijala.
// Sintetički nastavnik je angažovan na sintetičkom izvođenju samo za ovu demonstraciju;
// stvarna provera osoblja (ST-005) i prijava (ST-002) još nisu integrisane.
var demoTeacherId = Guid.NewGuid();
var demoOfferingId = Guid.NewGuid();
var createForm = new CreateMaterialForm(
    new CreateMaterialHandler(
        materials,
        new InMemoryOfferingStaffChecker([(demoTeacherId, demoOfferingId)])),
    Console.In,
    Console.Out);
await createForm.RunAsync(demoTeacherId, demoOfferingId);
Console.WriteLine();

var editForm = new EditMaterialDetailsForm(
    materials,
    new UpdateMaterialDetailsHandler(materials, TimeProvider.System),
    Console.In,
    Console.Out);
await editForm.RunAsync(demoMaterial.Id);

// ST-009, US-04: izbor i slanje datoteka u materijal, sa prikazom liste priloga.
// Sintetički nastavnik je angažovan na izvođenju demonstracionog materijala samo za ovu
// demonstraciju. Prilozi se čuvaju u privremenom direktorijumu, a podaci o njima u memoriji.
// Pravila tipa i veličine su ista kao u src/Api/appsettings.json (ADR-0002).
Console.WriteLine();
var attachments = new InMemoryMaterialAttachmentRepository();
var attachmentStorage = new LocalDiskAttachmentFileStorage(
    Path.Combine(Path.GetTempPath(), "studium-consoleui-attachments"));
var attachmentPolicy = new AttachmentUploadPolicy(
    [".pdf", ".docx", ".pptx", ".xlsx", ".txt", ".png", ".jpg", ".jpeg", ".zip"],
    20L * 1024 * 1024);
var uploadForm = new UploadAttachmentsForm(
    new UploadMaterialAttachmentsHandler(
        materials,
        attachments,
        new InMemoryOfferingStaffChecker([(demoTeacherId, demoMaterial.CourseOfferingId)]),
        attachmentPolicy,
        new AttachFileToMaterialHandler(materials, attachments, attachmentStorage, attachmentPolicy)),
    attachments,
    attachmentPolicy,
    Console.In,
    Console.Out);
await uploadForm.RunAsync(demoTeacherId, demoMaterial.Id);

Console.WriteLine();
Console.WriteLine("Sledeći korak: implementirati dodeljene zahteve iz specifikacije.");
