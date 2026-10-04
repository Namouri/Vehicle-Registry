using VehicleRegistry.Core.Entities;

namespace VehicleRegistry.Api.Dtos;
//formerna på datan som skickas ut och in till API
public record VehicleResponse(
    int Id,
    string RegistrationNumber,
    string Make,
    string Model,
    int Year,
    string Owner,
    VehicleStatus Status,
    DateTime? InspectionDue);

public record CreateVehicleRequest(
    string RegistrationNumber,
    string Make,
    string Model,
    int Year,
    string Owner,
    VehicleStatus Status,
    DateTime? InspectionDue);

public record UpdateVehicleRequest(
    string Owner,
    VehicleStatus Status,
    DateTime? InspectionDue);

public record VehicleHistoryEntryResponse(
    ChangeType ChangeType,
    string ChangedBy,
    DateTime ChangedAtUtc,
    string Owner,
    VehicleStatus Status);

public record AccessLogEntryResponse(
    string AccessedBy,
    string Reason,
    DateTime AccessedAtUtc);
