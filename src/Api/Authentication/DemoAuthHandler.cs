using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Application.Access;
using Infrastructure.Demo;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Api.Authentication;

public class DemoAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Demo";
    public const string UserHeader = "X-Demo-UserId";
    public static readonly string[] AllowedEnvironments = { "Development", "KT1" };

    private readonly IHostEnvironment _environment;

    public DemoAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IHostEnvironment environment) : base(options, logger, encoder)
    {
        _environment = environment;
    }

    public static bool IsAllowedEnvironment(IHostEnvironment environment) =>
        AllowedEnvironments.Any(environment.IsEnvironment);

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!IsAllowedEnvironment(_environment))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!Request.Headers.TryGetValue(UserHeader, out var values))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!Guid.TryParse(values.ToString(), out var userId) || userId == Guid.Empty)
        {
            return Task.FromResult(AuthenticateResult.Fail("Neispravan korisnički identifikator."));
        }

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        if (userId == DemoData.ErsTeacherId)
            claims.Add(new Claim(ClaimTypes.Role, nameof(UserRole.Teacher)));

        var identity = new ClaimsIdentity(claims, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
