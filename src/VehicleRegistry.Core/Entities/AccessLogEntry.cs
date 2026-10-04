namespace VehicleRegistry.Core.Entities;
//vem som läste ett fordon, när och värför. 
/// <summary>
/// Records a single read of a vehicle record: who looked, when, and why.
/// Written by RequireAccessReasonAttribute (see the Api project) before
/// the controller action runs, so even a request that ultimately fails is
/// still logged as an access attempt.
/// </summary>
public class AccessLogEntry
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public string AccessedBy { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime AccessedAtUtc { get; set; }
}
