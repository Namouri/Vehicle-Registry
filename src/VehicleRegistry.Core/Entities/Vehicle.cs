namespace VehicleRegistry.Core.Entities;
//hur ett fordon ser ut i nulöget. 
/// <summary>
/// The current state of a vehicle. This row is updated in place, the same
/// way a normal table would be. The full trail of how it got here lives in
/// VehicleHistory, written automatically whenever this entity is saved.
/// </summary>
public class Vehicle
{
    public int Id { get; set; }
    public string RegistrationNumber { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Owner { get; set; } = string.Empty;
    public VehicleStatus Status { get; set; }
    public DateTime? InspectionDue { get; set; }
}

public enum VehicleStatus
{
    InTraffic,
    Deregistered,
    Exported
}
