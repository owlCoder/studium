using System.Net;
using System.Net.Http.Json;
using Application.Access;
using Application.Auth;
using Application.Users;
using Domain.Users;
using Infrastructure.Users;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;

namespace Tests.Api;

[TestFixture]
public class DeactivateUserEndpointTests
{
    private static readonly Guid AdminId =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static readonly Guid StudentId =
        Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private const string AdminPassword = "admin-pass";
    private const string StudentPassword = "student-pass";

    private static TestApiFactory CreateFactory()
    {
        var hasher = new TestPasswordHasher();

        var repository = new InMemoryUserRepository(new[]
        {
            new User(
                AdminId,
                "Test",
                "Administrator",
                "admin.test",
                UserRole.Administrator,
                passwordHash: hasher.Hash(AdminPassword)),

            new User(
                StudentId,
                "Test",
                "Student",
                "student.test",
                UserRole.Student,
                passwordHash: hasher.Hash(StudentPassword))
        });

        return new TestApiFactory(repository, hasher);
    }

    private static async Task LoginAsync(
        HttpClient client,
        string username,
        string password)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username, password });

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.OK),
            "Prijava test korisnika nije uspela.");
    }

    [Test]
    public async Task AnonymousUser_CannotDeactivateAccount()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PutAsync(
            $"/users/{StudentId}/deactivate",
            null);

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Student_CannotDeactivateAccount()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        await LoginAsync(client, "student.test", StudentPassword);

        var response = await client.PutAsync(
            $"/users/{StudentId}/deactivate",
            null);

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Administrator_CanDeactivateStudent()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        await LoginAsync(client, "admin.test", AdminPassword);

        var response = await client.PutAsync(
            $"/users/{StudentId}/deactivate",
            null);

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task Administrator_CannotDeactivateLastActiveAdministrator()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        await LoginAsync(client, "admin.test", AdminPassword);

        var response = await client.PutAsync(
            $"/users/{AdminId}/deactivate",
            null);

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Conflict));
    }

    private sealed class TestApiFactory : WebApplicationFactory<global::Program>
    {
        private readonly InMemoryUserRepository _repository;
        private readonly IPasswordHasher _hasher;

        public TestApiFactory(
            InMemoryUserRepository repository,
            IPasswordHasher hasher)
        {
            _repository = repository;
            _hasher = hasher;
        }

        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<InMemoryUserRepository>();
                services.RemoveAll<IUserRepository>();
                services.RemoveAll<IUserLookup>();
                services.RemoveAll<IAccountReader>();
                services.RemoveAll<IPasswordHasher>();

                services.AddSingleton(_repository);
                services.AddSingleton<IUserRepository>(_repository);
                services.AddSingleton<IUserLookup>(_repository);
                services.AddSingleton<IAccountReader>(_repository);
                services.AddSingleton(_hasher);
            });
        }
    }

    private sealed class TestPasswordHasher : IPasswordHasher
    {
        public string Hash(string password)
        {
            return $"test-hash:{password}";
        }

        public bool Verify(string password, string passwordHash)
        {
            return passwordHash == Hash(password);
        }
    }
}
