using HoppyRoute.Domain.Enums;
using HoppyRoute.Domain.Entities;

namespace HoppyRoute.Application.DTOs
{
    public class VehicleDto
    {
        public int Id { get; set; }
        public string ExternalId { get; set; } = string.Empty;
        public string RegistrationNumber { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public int? ZoneId { get; set; }
        public string? ZoneName { get; set; }
        public int? CurrentParkingZoneId { get; set; }
        public string? CurrentParkingZoneName { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int BatteryLevel { get; set; }
        public bool NeedsBatteryReplacement { get; set; }
        public bool IsAvailable { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class ZoneDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public string GeoJsonBoundary { get; set; } = string.Empty;
        public int RegionId { get; set; }
        public string RegionName { get; set; } = string.Empty;
        
        // Statistics
        public int TotalVehicles { get; set; }
        public int VehiclesNeedingBatteryReplacement { get; set; }
        public int TotalParkingZones { get; set; }
        public List<ParkingZoneDto> ParkingZones { get; set; } = new List<ParkingZoneDto>();
    }

    public class SwapperDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
    }

    public class RouteDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int AssignedSwapperId { get; set; }
        public string AssignedSwapperName { get; set; } = string.Empty;
        public int CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; } = string.Empty;
        public int ZoneId { get; set; }
        public string ZoneName { get; set; } = string.Empty;
        public int? RegionId { get; set; }
        public string? RegionName { get; set; }
        public DateTime Date { get; set; }
        public int TargetDurationMinutes { get; set; }
        public double? EstimatedDistanceKm { get; set; }
        public int TotalVehicleCount { get; set; }
        public RouteStatus Status { get; set; }
        public RouteApprovalStatus ApprovalStatus { get; set; }
        public RouteType? Type { get; set; }
        public int? ApprovedBy { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ManagerNotes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public List<RouteStopDto> Stops { get; set; } = new List<RouteStopDto>();
    }

    public class RouteStopDto
    {
        public int Id { get; set; }
        public int RouteId { get; set; }
        public int VehicleId { get; set; }
        public VehicleDto Vehicle { get; set; } = null!;
        public int Order { get; set; }
        public RouteStopType StopType { get; set; }
        public int? PickupParkingZoneId { get; set; }
        public string? PickupParkingZoneName { get; set; }
        public int? DropoffParkingZoneId { get; set; }
        public string? DropoffParkingZoneName { get; set; }
        public TimeSpan EstimatedArrivalOffset { get; set; }
        public TimeSpan EstimatedDurationAtStop { get; set; }
        public RouteStopStatus Status { get; set; }
        public DateTime? ActualArrivalTime { get; set; }
        public DateTime? ActualDepartureTime { get; set; }
        public string? Notes { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
