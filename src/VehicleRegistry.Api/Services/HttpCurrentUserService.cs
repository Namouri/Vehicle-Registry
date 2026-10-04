using VehicleRegistry.Core.Services;

namespace VehicleRegistry.Api.Services;
//den faktsiksa koden som läser ut vem som är inloggad. 
/// <summary>
/// Stand-in for real authentication. Reads a "X-User" header so the
/// project is runnable and testable without a login system. Swapping
/// this for JWT-based identity later means implementing ICurrentUserService
/// against HttpContext.User instead - nothing in Core or in the
/// interceptor needs to change, which is the point of depending on the
/// interface rather than on ASP.NET Core identity directly.
/// </summary>
public class HttpCurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string UserName =>
        _httpContextAccessor.HttpContext?.Request.Headers["X-User"].FirstOrDefault()
        ?? "anonymous";
}
