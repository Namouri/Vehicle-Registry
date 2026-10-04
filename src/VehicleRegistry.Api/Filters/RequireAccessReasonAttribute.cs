using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using VehicleRegistry.Core.Data;
using VehicleRegistry.Core.Entities;
using VehicleRegistry.Core.Services;

namespace VehicleRegistry.Api.Filters;

/// <summary>
/// Applied to any action that reads a vehicle record. Requires a
/// "X-Access-Reason" header; if it's missing, the request is rejected
/// before the controller action runs. If present, the access is recorded
/// in AccessLogEntries - including for requests that later 404, since the
/// act of *attempting* to look someone up is itself worth logging.
///
/// This is implemented as an action filter rather than middleware because
/// it needs the vehicle id from the route, and DI-injected filters get
/// that from ActionContext for free. Middleware would need to parse the
/// route manually, which is more fragile.
/// </summary>
public class RequireAccessReasonAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;

        if (!request.Headers.TryGetValue("X-Access-Reason", out var reasonValues)
            || string.IsNullOrWhiteSpace(reasonValues.FirstOrDefault()))
        {
            context.Result = new BadRequestObjectResult(new
            {
                error = "An access reason is required. Set the 'X-Access-Reason' header."
            });
            return;
        }

        var reason = reasonValues.First()!;

        if (!context.RouteData.Values.TryGetValue("id", out var idValue)
            || !int.TryParse(idValue?.ToString(), out var vehicleId))
        {
            // No vehicle id on the route - nothing to log against, let the
            // action run and handle its own validation.
            await next();
            return;
        }

        var dbContext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        var currentUser = context.HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();

        dbContext.AccessLogEntries.Add(new AccessLogEntry
        {
            VehicleId = vehicleId,
            AccessedBy = currentUser.UserName,
            Reason = reason,
            AccessedAtUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        await next();
    }
}
