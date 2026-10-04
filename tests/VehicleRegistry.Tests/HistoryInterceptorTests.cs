using Microsoft.EntityFrameworkCore;
using VehicleRegistry.Core.Data;
using VehicleRegistry.Core.Entities;
using VehicleRegistry.Core.Services;
using Xunit;

namespace VehicleRegistry.Tests;

/// <summary>
/// A fake ICurrentUserService so tests control exactly who "made" each
/// change, without needing HTTP context.
/// </summary>
public class FakeCurrentUserService : ICurrentUserService
{
    public string UserName { get; set; } = "test-user";
}

public class HistoryInterceptorTests
{
    private static (AppDbContext db, FakeCurrentUserService user) CreateContext(string dbName)
    {
        var user = new FakeCurrentUserService();
        var interceptor = new HistorySaveChangesInterceptor(user);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .AddInterceptors(interceptor)
            .Options;

        return (new AppDbContext(options), user);
    }

    [Fact]
    public async Task Creating_a_vehicle_writes_one_history_row_of_type_Created()
    {
        var (db, _) = CreateContext(nameof(Creating_a_vehicle_writes_one_history_row_of_type_Created));

        db.Vehicles.Add(new Vehicle
        {
            RegistrationNumber = "ABC123",
            Make = "Volvo",
            Model = "V60",
            Year = 2019,
            Owner = "Karin Lindqvist",
            Status = VehicleStatus.InTraffic
        });
        await db.SaveChangesAsync();

        var history = await db.VehicleHistories.ToListAsync();

        Assert.Single(history);
        Assert.Equal(ChangeType.Created, history[0].ChangeType);
        Assert.Equal("Karin Lindqvist", history[0].Owner);
    }

    [Fact]
    public async Task Updating_a_vehicle_adds_a_new_history_row_without_removing_the_old_one()
    {
        var (db, user) = CreateContext(nameof(Updating_a_vehicle_adds_a_new_history_row_without_removing_the_old_one));

        var vehicle = new Vehicle
        {
            RegistrationNumber = "ABC123",
            Make = "Volvo",
            Model = "V60",
            Year = 2019,
            Owner = "Jonas Berg",
            Status = VehicleStatus.InTraffic
        };
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();

        user.UserName = "clerk.eriksson";
        vehicle.Owner = "Karin Lindqvist";
        await db.SaveChangesAsync();

        var history = await db.VehicleHistories
            .OrderBy(h => h.ChangedAtUtc)
            .ToListAsync();

        Assert.Equal(2, history.Count);
        Assert.Equal("Jonas Berg", history[0].Owner);
        Assert.Equal("Karin Lindqvist", history[1].Owner);
        Assert.Equal("clerk.eriksson", history[1].ChangedBy);
    }

    [Fact]
    public async Task AsOf_query_returns_the_state_that_was_current_at_that_time()
    {
        var (db, _) = CreateContext(nameof(AsOf_query_returns_the_state_that_was_current_at_that_time));

        var vehicle = new Vehicle
        {
            RegistrationNumber = "ABC123",
            Make = "Volvo",
            Model = "V60",
            Year = 2019,
            Owner = "Jonas Berg",
            Status = VehicleStatus.InTraffic
        };
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();
        var afterCreate = DateTime.UtcNow;

        await Task.Delay(10); // ensure the timestamps are distinguishable
        vehicle.Owner = "Karin Lindqvist";
        await db.SaveChangesAsync();

        // Querying "as of" a moment right after creation, but before the
        // update, should return the original owner - this is the
        // behaviour the /history "time travel" endpoint relies on.
        var snapshotAtCreation = await db.VehicleHistories
            .Where(h => h.VehicleId == vehicle.Id && h.ChangedAtUtc <= afterCreate)
            .OrderByDescending(h => h.ChangedAtUtc)
            .FirstOrDefaultAsync();

        Assert.NotNull(snapshotAtCreation);
        Assert.Equal("Jonas Berg", snapshotAtCreation!.Owner);
    }
}
