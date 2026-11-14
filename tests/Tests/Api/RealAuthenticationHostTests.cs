using System.Net;
using System.Net.Http.Json;
using Infrastructure.Demo;
using NUnit.Framework;

namespace Tests.Api;

/// <summary>
/// Provera prava i identiteta nad HTTP hostom sa autentifikacijom koju Program.cs stvarno
/// registruje (bez TestAuthenticationHandler-a). Ovaj modul je KT1-only: nema toka izdavanja
/// identiteta, pa nijedan zahtev ne sme da dobije identitet, ni anonimno, ni laznim
/// test zaglavljima, ni laznim cookie-jem. Svi zasticeni endpointi moraju da vrate 401.
///
/// Napomena: 403 se na ovom hostu ne moze dobiti jer identitet ne postoji; vlasnistvo nad
/// grupom i tudji sadrzaj (403) proveravaju testovi sa test autentifikacijom
/// (TestoviGrupa, ApiRoutesTests). Koristi se TestServer, a ne Kestrel.
/// </summary>
[TestFixture]
public class RealAuthenticationHostTests
{
    private const string AnonymousVariant = "anonymous";
    private const string ForgedTestHeadersVariant = "forged-test-headers";
    private const string ForgedCookieVariant = "forged-cookie";

    private static IEnumerable<TestCaseData> ProtectedRoutes()
    {
        var group = DemoData.LecturesGroupId;
        var student = DemoData.ActiveStudentId;
        var course = DemoData.ErsCourseId;

        foreach (var environment in new[] { "Development", "Production" })
        {
            yield return new TestCaseData(environment, "POST", $"/groups/{group}/students");
            yield return new TestCaseData(environment, "DELETE", $"/groups/{group}/students/{student}");
            yield return new TestCaseData(environment, "POST", $"/api/Groups/{group}/students");
            yield return new TestCaseData(environment, "DELETE", $"/api/Groups/{group}/students/{student}");
            yield return new TestCaseData(environment, "GET", $"/api/Groups/students/{student}/groups");
            yield return new TestCaseData(environment, "GET", $"/api/courses/{course}/content?studentId={student}");
        }

        // ST-020 ruta postoji samo u Development okruzenju (u Production nije mapirana).
        yield return new TestCaseData("Development", "POST", $"/contents/{Guid.NewGuid()}/target-audience");
    }

    private static HttpRequestMessage BuildRequest(string method, string url, string variant)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), url);

        if (method == "POST")
        {
            request.Content = JsonContent.Create(new { StudentId = DemoData.ActiveStudentId, StartDate = DateTime.UtcNow });
        }

        switch (variant)
        {
            case ForgedTestHeadersVariant:
                request.Headers.Add("X-Test-User", Guid.NewGuid().ToString());
                request.Headers.Add("X-Test-Role", ApiTestHost.AdminRoleHeaderValue);
                break;

            case ForgedCookieVariant:
                request.Headers.TryAddWithoutValidation(
                    "Cookie",
                    ".AspNetCore.TestScheme=" + Convert.ToBase64String(Guid.NewGuid().ToByteArray()));
                break;
        }

        return request;
    }

    [TestCaseSource(nameof(ProtectedRoutes))]
    public async Task ProtectedRoute_NeverGrantsIdentityOnRealHost_ReturnsUnauthorized(
        string environment, string method, string url)
    {
        using var factory = ApiTestHost.CreateWithApplicationAuthentication(environment);
        using var client = factory.CreateClient();

        foreach (var variant in new[] { AnonymousVariant, ForgedTestHeadersVariant, ForgedCookieVariant })
        {
            using var request = BuildRequest(method, url, variant);

            var response = await client.SendAsync(request);

            Assert.That(
                response.StatusCode,
                Is.EqualTo(HttpStatusCode.Unauthorized),
                $"{method} {url} [{environment}, {variant}]");
        }
    }

    [TestCase("Development", "/health")]
    [TestCase("Development", "/demo/courses")]
    [TestCase("Development", "/courses")]
    [TestCase("Production", "/health")]
    [TestCase("Production", "/demo/courses")]
    [TestCase("Production", "/courses")]
    public async Task PublicRoutes_StayAnonymousOnRealHost(string environment, string url)
    {
        using var factory = ApiTestHost.CreateWithApplicationAuthentication(environment);
        using var client = factory.CreateClient();

        var response = await client.GetAsync(url);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}