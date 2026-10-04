using Microsoft.EntityFrameworkCore;
using VehicleRegistry.Core.Entities;

namespace VehicleRegistry.Core.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<VehicleHistory> VehicleHistories => Set<VehicleHistory>();
    public DbSet<AccessLogEntry> AccessLogEntries => Set<AccessLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Vehicle>(e =>
        {
            e.HasIndex(v => v.RegistrationNumber).IsUnique();
        });

        modelBuilder.Entity<VehicleHistory>(e =>
        {
            // History rows are append-only: index by (VehicleId, ChangedAtUtc)
            // since that's exactly the lookup the "as of" query performs -
            // "give me the newest row for this vehicle at or before date X".
            // Deliberately no foreign key relationship here - history must
            // survive the deletion of the vehicle it describes, so nothing
            // should ever block a delete because history still exists.
            e.HasIndex(h => new { h.VehicleId, h.ChangedAtUtc });
        });

        modelBuilder.Entity<AccessLogEntry>(e =>
        {
            e.HasIndex(a => a.VehicleId);
        });
    }
}