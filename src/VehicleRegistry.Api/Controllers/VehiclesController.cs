using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleRegistry.Api.Dtos;
using VehicleRegistry.Api.Filters;
using VehicleRegistry.Core.Data;
using VehicleRegistry.Core.Entities;

namespace VehicleRegistry.Api.Controllers;

[ApiController]
[Route("api/vehicles")]
public class VehiclesController : ControllerBase
{
    private readonly AppDbContext _db;

    public VehiclesController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// List all vehicles. Deliberately not behind RequireAccessReason - a
    /// list of registration numbers isn't personal data on its own, unlike
    /// opening one specific owner's record. Worth naming as a conscious
    /// choice if asked.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<VehicleResponse>>> GetAll()
    {
        var vehicles = await _db.Vehicles.ToListAsync();
        return Ok(vehicles.Select(ToResponse).ToList());
    }

    /// <summary>
    /// Get a vehicle. Requires an X-Access-Reason header (enforced by the
    /// filter). Pass ?asOf=2025-03-01 to see the record as it looked at
    /// that date instead of its current state.
    /// </summary>
    [HttpGet("{id:int}")]
    [RequireAccessReason]
    public async Task<ActionResult<VehicleResponse>> Get(int id, [FromQuery] DateTime? asOf)
    {
        if (asOf is null)
        {
            var vehicle = await _db.Vehicles.FindAsync(id);
            if (vehicle is null) return NotFound();
            return Ok(ToResponse(vehicle));
        }

        // Reconstruct historical state: the newest history row for this
        // vehicle at or before the requested date. This is the entire
        // "time travel" feature - one indexed query, no replay logic
        // needed, because we store full snapshots rather than diffs.
        var asOfUtc = DateTime.SpecifyKind(asOf.Value, DateTimeKind.Utc);
        var snapshot = await _db.VehicleHistories
            .Where(h => h.VehicleId == id && h.ChangedAtUtc <= asOfUtc)
            .OrderByDescending(h => h.ChangedAtUtc)
            .FirstOrDefaultAsync();

        if (snapshot is null) return NotFound();
        if (snapshot.ChangeType == ChangeType.Deleted) return NotFound();

        return Ok(new VehicleResponse(
            snapshot.VehicleId, snapshot.RegistrationNumber, snapshot.Make,
            snapshot.Model, snapshot.Year, snapshot.Owner, snapshot.Status,
            snapshot.InspectionDue));
    }

    /// <summary>Full change history for a vehicle, newest first.</summary>
    [HttpGet("{id:int}/history")]
    [RequireAccessReason]
    public async Task<ActionResult<List<VehicleHistoryEntryResponse>>> GetHistory(int id)
    {
        var history = await _db.VehicleHistories
            .Where(h => h.VehicleId == id)
            .OrderByDescending(h => h.ChangedAtUtc)
            .Select(h => new VehicleHistoryEntryResponse(
                h.ChangeType, h.ChangedBy, h.ChangedAtUtc, h.Owner, h.Status))
            .ToListAsync();

        return Ok(history);
    }

    /// <summary>Who has accessed this record, when, and why.</summary>
    [HttpGet("{id:int}/access-log")]
    [RequireAccessReason]
    public async Task<ActionResult<List<AccessLogEntryResponse>>> GetAccessLog(int id)
    {
        var log = await _db.AccessLogEntries
            .Where(a => a.VehicleId == id)
            .OrderByDescending(a => a.AccessedAtUtc)
            .Select(a => new AccessLogEntryResponse(a.AccessedBy, a.Reason, a.AccessedAtUtc))
            .ToListAsync();

        return Ok(log);
    }

    [HttpPost]
    public async Task<ActionResult<VehicleResponse>> Create(CreateVehicleRequest request)
    {
        var vehicle = new Vehicle
        {
            RegistrationNumber = request.RegistrationNumber,
            Make = request.Make,
            Model = request.Model,
            Year = request.Year,
            Owner = request.Owner,
            Status = request.Status,
            InspectionDue = request.InspectionDue
        };

        _db.Vehicles.Add(vehicle);
        // The history interceptor fires inside this call and writes a
        // "Created" row automatically - no explicit call needed here.
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = vehicle.Id }, ToResponse(vehicle));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateVehicleRequest request)
    {
        var vehicle = await _db.Vehicles.FindAsync(id);
        if (vehicle is null) return NotFound();

        vehicle.Owner = request.Owner;
        vehicle.Status = request.Status;
        vehicle.InspectionDue = request.InspectionDue;

        // Again, no explicit history call - the interceptor writes a
        // "Modified" snapshot as part of this save.
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var vehicle = await _db.Vehicles.FindAsync(id);
        if (vehicle is null) return NotFound();

        _db.Vehicles.Remove(vehicle);
        // The interceptor writes a "Deleted" history row here automatically,
        // after the delete completes - see HistorySaveChangesInterceptor.
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static VehicleResponse ToResponse(Vehicle v) => new(
        v.Id, v.RegistrationNumber, v.Make, v.Model, v.Year, v.Owner, v.Status, v.InspectionDue);
}