using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VehicleRegistry.Core.Entities;
using VehicleRegistry.Core.Services;

namespace VehicleRegistry.Core.Data;

/// <summary>
/// Writes a matching, immutable VehicleHistory row for every Vehicle that
/// is added, modified, or deleted - but does so in two steps:
///
/// 1. SavingChanges(Async) runs just BEFORE the save. At this point we
///    simply note which Vehicle entries are changing and how (their
///    EntityState). We can't build the final history rows yet, because a
///    brand new vehicle doesn't have its real database Id assigned until
///    the save actually happens.
/// 2. SavedChanges(Async) runs just AFTER the save completes. By now every
///    vehicle - including new ones - has its real Id. We build the
///    history rows here and perform a small second SaveChanges to persist
///    them. This only ever touches VehicleHistory rows, so it doesn't
///    trigger this same logic again (the filter below only looks at
///    Vehicle entities).
///
/// Why an interceptor at all, rather than doing this in the controller?
/// Because it runs no matter which code path calls SaveChanges - a
/// controller action, a background job, a script - so history can never
/// be accidentally skipped.
/// </summary>
public class HistorySaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUser;
    private List<(Vehicle Vehicle, EntityState State)> _pending = new();

    public HistorySaveChangesInterceptor(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        CapturePendingChanges(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        CapturePendingChanges(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        WriteHistoryEntries(eventData.Context);
        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result,
        CancellationToken cancellationToken = default)
    {
        await WriteHistoryEntriesAsync(eventData.Context, cancellationToken);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private void CapturePendingChanges(DbContext? context)
    {
        if (context is null) return;

        // Snapshot which vehicles are changing, and how, before the save
        // runs. We keep the Vehicle object itself (not just its Id) so
        // that after the save we still have its final values to copy into
        // the history row - including, for new vehicles, the Id that
        // doesn't exist yet at this point.
        _pending = context.ChangeTracker
            .Entries<Vehicle>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(e => (e.Entity, e.State))
            .ToList();
    }

    private void WriteHistoryEntries(DbContext? context)
    {
        if (context is null || _pending.Count == 0) return;

        var rows = BuildHistoryRows();
        context.Set<VehicleHistory>().AddRange(rows);
        _pending.Clear();

        // A second, small save - only VehicleHistory rows are pending now,
        // so this does not re-trigger the logic above.
        context.SaveChanges();
    }

    private async Task WriteHistoryEntriesAsync(DbContext? context, CancellationToken cancellationToken)
    {
        if (context is null || _pending.Count == 0) return;

        var rows = BuildHistoryRows();
        context.Set<VehicleHistory>().AddRange(rows);
        _pending.Clear();

        await context.SaveChangesAsync(cancellationToken);
    }

    private List<VehicleHistory> BuildHistoryRows()
    {
        var now = DateTime.UtcNow;
        var user = _currentUser.UserName;

        return _pending.Select(p => new VehicleHistory
        {
            VehicleId = p.Vehicle.Id,   // real Id is guaranteed set by now, for every state
            RegistrationNumber = p.Vehicle.RegistrationNumber,
            Make = p.Vehicle.Make,
            Model = p.Vehicle.Model,
            Year = p.Vehicle.Year,
            Owner = p.Vehicle.Owner,
            Status = p.Vehicle.Status,
            InspectionDue = p.Vehicle.InspectionDue,
            ChangeType = p.State switch
            {
                EntityState.Added => ChangeType.Created,
                EntityState.Deleted => ChangeType.Deleted,
                _ => ChangeType.Modified
            },
            ChangedBy = user,
            ChangedAtUtc = now
        }).ToList();
    }
}