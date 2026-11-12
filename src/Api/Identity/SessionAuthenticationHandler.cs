using System.Security.Claims;
using System.Text.Encodings.Web;
using Application.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Api.Identity;

/// <summary>
/// US-7.5: šema autentifikacije zasnovana na sesiji.
/// Iz zahteva se čita SAMO token sesije iz zaglavlja "Authorization: Bearer ...".
/// ID korisnika se nikad ne čita iz zahteva (query, ruta, telo, zaglavlja),
/// već ga daje sesija, i to samo ako je nalog aktivan.
/// </summary>
public sealed class SessionAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "StudiumSession";

    private const string BearerPrefix = "Bearer ";

    private readonly ResolveSessionUserHandler _resolveSessionUser;

    public SessionAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ResolveSessionUserHandler resolveSessionUser)
        : base(options, logger, encoder)
    {
        _resolveSessionUser = resolveSessionUser;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = ReadBearerToken(Request);
        if (token is null)
        {
            return AuthenticateResult.NoResult();
        }

        var userId = await _resolveSessionUser.HandleAsync(token, Context.RequestAborted);
        if (userId is null)
        {
            // Ista poruka za nevažeću sesiju i neaktivan nalog, da se ne otkriva razlog.
            return AuthenticateResult.Fail("Sesija ne važi.");
        }

        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()) },
            SchemeName);

        return AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }

    private static string? ReadBearerToken(HttpRequest request)
    {
        var header = request.Headers.Authorization.ToString();
        if (!header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var token = header[BearerPrefix.Length..].Trim();
        return token.Length == 0 ? null : token;
    }
}
