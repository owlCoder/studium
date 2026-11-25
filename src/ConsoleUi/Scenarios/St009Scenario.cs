using Application.Materials;
using ConsoleUi.Materials;
using Domain.Materials;
using Infrastructure.Access;
using Infrastructure.Materials;

namespace ConsoleUi.Scenarios;

/// <summary>
/// Demonstracija ST-009 (nastavni materijali) nad sintetičkim podacima u memoriji:
/// kreiranje i izmena materijala, otpremanje priloga, spoljne veze, objava materijala
/// iz liste za nastavnika i lista materijala za studenta.
/// Pokretanje: <c>dotnet run --project src/ConsoleUi -- st-009</c>.
/// Stvarna provera osoblja (ST-005) i prijava (ST-002) još nisu integrisane.
/// </summary>
public static class St009Scenario
{
    public static async Task RunAsync()
    {
        // ST-009, US-02: forma za izmenu naziva i opisa nad sintetičkim materijalom u memoriji.
        // Podaci se ne čuvaju trajno; provera nastavnika izvođenja (ST-005) nije uključena.
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

        // Sintetički nastavnik je angažovan na izvođenju demonstracionog materijala.
        var editForm = new EditMaterialDetailsForm(
            materials,
            new UpdateMaterialDetailsHandler(
                materials,
                new InMemoryOfferingStaffChecker([(demoTeacherId, demoMaterial.CourseOfferingId)]),
                TimeProvider.System),
            Console.In,
            Console.Out);
        await editForm.RunAsync(demoTeacherId, demoMaterial.Id);

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

        // ST-009, US-03: unos spoljne veze u demonstracioni materijal i prikaz liste veza.
        // Veze se čuvaju samo u memoriji; sintetički nastavnik je angažovan na izvođenju materijala.
        Console.WriteLine();
        var externalLinks = new InMemoryMaterialExternalLinkRepository();
        var addLinkHandler = new AddMaterialExternalLinkHandler(
            materials,
            externalLinks,
            new InMemoryOfferingStaffChecker([(demoTeacherId, demoMaterial.CourseOfferingId)]));
        var enteredLink = new ExternalLinkField(Console.In, Console.Out).Read(demoMaterial.Id);
        if (enteredLink is not null)
        {
            try
            {
                await addLinkHandler.HandleAsync(
                    new AddMaterialExternalLinkCommand(demoTeacherId, demoMaterial.Id, enteredLink.Url.AbsoluteUri));
                Console.WriteLine("Veza je sačuvana.");
            }
            catch (MaterialAccessDeniedException ex)
            {
                Console.WriteLine($"Veza nije sačuvana: {ex.Message}");
            }
        }
        new ExternalLinkListView(Console.Out).Show(
            demoMaterial.Id,
            await externalLinks.ListByMaterialAsync(demoMaterial.Id));

        // ST-009, US-08: lista materijala izvođenja za nastavnika sa oznakom stanja i dugmetom
        // „Objavi” uz nacrt. Objava se odbija ako nekom prilogu nedostaje sačuvana datoteka.
        // Sintetički nastavnik je angažovan na izvođenju demonstracionog materijala.
        Console.WriteLine();
        var demoStaffChecker = new InMemoryOfferingStaffChecker([(demoTeacherId, demoMaterial.CourseOfferingId)]);
        var staffMaterialList = new StaffMaterialListForm(
            new ListMaterialsForStaffHandler(materials, demoStaffChecker),
            new PublishMaterialHandler(materials, attachments, attachmentStorage, demoStaffChecker),
            Console.In,
            Console.Out);
        await staffMaterialList.RunAsync(demoTeacherId, demoMaterial.CourseOfferingId);

        // ST-009, US-10: lista materijala predmeta za studenta (naziv i kratak opis).
        // Sintetički student je upisan na izvođenje demonstracionog materijala samo za ovu
        // demonstraciju; stvarna članstva (ST-006) još nisu povezana. Prikazuje se samo
        // objavljen materijal, pa se nacrt iz forme za kreiranje ne vidi.
        // Ako nastavnik nije objavio materijal u listi iznad, demonstracija ga objavljuje ovde.
        Console.WriteLine();
        if (demoMaterial.PublicationState == MaterialPublicationState.Draft)
        {
            demoMaterial.Publish();
            await materials.UpdateAsync(demoMaterial);
        }
        var demoStudentId = Guid.NewGuid();
        var studentMaterialList = new StudentMaterialListView(
            new ListPublishedMaterialsForStudentHandler(
                materials,
                new InMemoryMembershipReader(
                [
                    new OfferingMembership(demoStudentId, demoMaterial.CourseOfferingId, MembershipKind.Student, true)
                ])),
            Console.Out);
        await studentMaterialList.ShowAsync(demoStudentId, demoMaterial.CourseOfferingId);
    }
}
