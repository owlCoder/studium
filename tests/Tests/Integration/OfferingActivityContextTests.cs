using System.Net;
using System.Net.Http.Json;
using Application.Activities;
using Application.Courses;
using Domain.Offerings;
using Infrastructure.Activities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;

namespace Tests.Integration;

/// <summary>
/// ST-021 + ST-004: dokaz da aktivnosti mogu da pripadaju izvođenju predmeta bez izmene
/// ruta, rukovalaca, domena i repozitorijuma — menja se samo implementacija porta
/// <see cref="IActivityContextReader"/>. Izvođenja se u testu zadaju lokalno, pa ovaj
/// prototip ne dira ST-004 demo podatke, portove ni registracije u Program.cs.
/// </summary>
[TestFixture]
public sealed class OfferingActivityContextTests
{
    private static readonly Guid ErsId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OibId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ErsOffering = Guid.Parse("33333333-0000-0000-0000-000000000001");
    private static readonly Guid OibOffering = Guid.Parse("33333333-0000-0000-0000-000000000002");
    private static readonly DateTime Start = new(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime End = new(2026, 3, 15, 23, 59, 0, DateTimeKind.Utc);

    [Test]
    public async Task CreateActivity_WithOfferingSlug_StoresOfferingAsContext()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/Course/ERS-2026-27-00001/CreateActivity", new
        {
            name = "Projekat (izvođenje)",
            description = "Aktivnost vezana za izvođenje predmeta",
            startDate = Start,
            endDate = End
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var created = await response.Content.ReadFromJsonAsync<ActivityResponse>();
        Assert.Multiple(() =>
        {
            Assert.That(created!.ContextId, Is.EqualTo(ErsOffering));
            Assert.That(created.Name, Is.EqualTo("Projekat (izvođenje)"));
        });
    }

    [Test]
    public async Task CreateActivity_WithCatalogCourseSlug_IsRejectedWhenOfferingsResolveTheContext()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/Course/ERS-11111/CreateActivity", new
        {
            name = "Projekat (katalog)",
            description = "Oznaka predmeta, ne izvođenja",
            startDate = Start,
            endDate = End
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task ListActivities_ReturnsActivityStoredForOffering()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        await client.PostAsJsonAsync("/Course/OIB-2026-27-00002/CreateActivity", new
        {
            name = "Vežbe",
            description = "Uz izvođenje OIB",
            startDate = Start,
            endDate = End
        });

        var activities = await client.GetFromJsonAsync<List<ActivityResponse>>("/ListActivities");

        Assert.That(activities, Has.Exactly(1).Matches<ActivityResponse>(activity =>
            activity.ContextId == OibOffering && activity.Name == "Vežbe"));
    }

    private static WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IActivityContextReader>();
                services.AddSingleton<IActivityContextReader>(provider =>
                    new OfferingActivityContextReader(
                        provider.GetRequiredService<ICourseReader>(),
                        new[]
                        {
                            new Offering(ErsOffering, ErsId, "2026/27"),
                            new Offering(OibOffering, OibId, "2026/27")
                        }));
            }));

    private sealed record ActivityResponse(Guid Id, Guid ContextId, string Name);
}
