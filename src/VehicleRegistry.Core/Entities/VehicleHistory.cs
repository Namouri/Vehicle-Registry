namespace VehicleRegistry.Core.Entities;

/// <summary>
/// A full snapshot of a Vehicle at one point in time. A new row is written
/// every time the vehicle is created or changed - existing rows are never
/// updated or deleted. This is what makes "what did the record look like
/// on date X" answerable: find the newest row with ChangedAtUtc <= X.
///
/// Storing the full snapshot (rather than just the changed field) is a
/// deliberate simplification. It uses a bit more storage, but makes the
/// "as of" query a single lookup instead of replaying a sequence of
/// field-level diffs. That trade-off is worth explaining if asked.
/// </summary>
public class VehicleHistory
{
    public int Id { get; set; }

    // Deliberately a plain int, with no foreign key relationship to
    // Vehicle (see AppDbContext - no HasOne/WithMany for this). History
    // must be able to outlive the vehicle it describes, so the database
    // must never refuse to delete a vehicle just because history rows
    // still reference its Id.
    public int VehicleId { get; set; }

    public string RegistrationNumber { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Owner { get; set; } = string.Empty;
    public VehicleStatus Status { get; set; }
    public DateTime? InspectionDue { get; set; }

    public ChangeType ChangeType { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAtUtc { get; set; }
}

public enum ChangeType
{
    Created,
    Modified,
    Deleted
}