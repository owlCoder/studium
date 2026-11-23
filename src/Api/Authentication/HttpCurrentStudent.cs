using System.Security.Claims;
using Application.Submissions;

namespace Api.Authentication;

public sealed class HttpCurrentStudent : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContext;

    public HttpCurrentStudent(IHttpContextAccessor httpContext)
    {
        _httpContext = httpContext;
    }

    public Guid StudentId
    {
        get
        {
            var user = _httpContext.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true &&
                Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id))
            {
                return id;
            }

            return Guid.Empty;
        }
    }
}
