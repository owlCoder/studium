using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Tests.Application;

namespace Tests.Api;

/// <summary>
/// Podize stvarni composition root (Program.cs) u zadatom okruzenju. Zamenjuje se samo
/// autentifikacija; provere postojanja grupe i statusa studenta ostaju one koje aplikacija
/// stvarno registruje, da bi testovi pokrili produkcionu konfiguraciju, a ne test dvojnike.
/// </summary>
internal static class ApiTestHost
{
    public const string AdminRoleHeaderValue = "Admin";

    public static WebApplicationFactory<Program> Create(
        string environment,
        Action<IServiceCollection>? configureServices = null)
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);

            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                    options.DefaultForbidScheme = "Test";
                })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });

                configureServices?.Invoke(services);
            });
        });
    }

    /// <summary>Zahtev ulogovanog administratora (X-Test-User + X-Test-Role).</summary>
    public static HttpRequestMessage AdminRequest(HttpMethod method, string url, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-Test-User", Guid.NewGuid().ToString());
        request.Headers.Add("X-Test-Role", AdminRoleHeaderValue);

        if (body != null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }
}
