namespace VehicleRegistry.Core.Services;
//säger vem som är inloggad. 
/// <summary>
/// Abstracts "who is making this request" away from HTTP. The interceptor
/// and the access-log filter both depend on this interface, not on
/// ASP.NET Core directly, so the Core project stays free of web
/// dependencies and the whole auth mechanism can be replaced later
/// (e.g. swapped for real JWT-based identity) without touching history
/// or logging logic.
/// </summary>
public interface ICurrentUserService
{
    string UserName { get; }
}
