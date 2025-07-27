using System.ComponentModel.DataAnnotations;
using HoppyRoute.Domain.Entities;

namespace HoppyRoute.Application.DTOs
{
    public class RouteGenerationRequest
    {
        [Required]
        public int AssignedSwapperId { get; set; }
        
        [Required]
        public int ZoneId { get; set; }
        
        public int? RegionId { get; set; }
        
        [Required]
        [Range(60, 1440)]
        public int TargetDurationMinutes { get; set; }
        
        [Range(1, 100)]
        public int? BatteryThreshold { get; set; } = 25; // Default 25%
        
        public DateTime? StartTime { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        [StringLength(50)]
        public string? RouteType { get; set; }
    }

    public class RouteGenerationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public RouteDto? Route { get; set; }
        public int TotalVehicles { get; set; }
        public int EstimatedDurationMinutes { get; set; }
        public double TotalDistanceKm { get; set; }
        public List<VehicleDto> SelectedVehicles { get; set; } = new List<VehicleDto>();
        public List<ParkingZoneDto> InvolvedParkingZones { get; set; } = new List<ParkingZoneDto>();
    }

    public class LowBatteryVehiclesRequest
    {
        [Required]
        public int ZoneId { get; set; }
        
        public int? RegionId { get; set; }
        
        [Range(1, 100)]
        public int BatteryThreshold { get; set; } = 25;
        
        public bool IncludeUnavailable { get; set; } = false;
    }

    public class RouteOptimizationResult
    {
        public List<int> OptimalOrder { get; set; } = new List<int>();
        public int TotalDurationMinutes { get; set; }
        public double TotalDistanceKm { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class CreateRouteRequest
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        public int AssignedSwapperId { get; set; }
        
        [Required]
        public int ZoneId { get; set; }
        
        public int? RegionId { get; set; }
        
        [Required]
        public DateTime Date { get; set; }
        
        [Required]
        [Range(60, 1440)]
        public int TargetDurationMinutes { get; set; }
        
        [Required]
        public List<int> VehicleIds { get; set; } = new List<int>();
        
        public List<RouteStopRequest> CustomStops { get; set; } = new List<RouteStopRequest>();
        
        [StringLength(50)]
        public string? RouteType { get; set; }
    }

    public class RouteStopRequest
    {
        [Required]
        public int VehicleId { get; set; }
        
        [Required]
        public RouteStopType StopType { get; set; }
        
        public int? PickupParkingZoneId { get; set; }
        
        public int? DropoffParkingZoneId { get; set; }
        
        [Range(1, int.MaxValue)]
        public int Order { get; set; }
        
        [StringLength(500)]
        public string? Notes { get; set; }
    }

    public class UpdateRouteRequest
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        public DateTime Date { get; set; }
        
        [Required]
        [Range(60, 1440)]
        public int TargetDurationMinutes { get; set; }
        
        public double? EstimatedDistanceKm { get; set; }
    }

    public class AssignUserToRegionRequest
    {
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public int RegionId { get; set; }
    }

    public class GetVehiclesInRegionRequest
    {
        [Required]
        public int RegionId { get; set; }
        
        public int? ZoneId { get; set; }
        
        public int? BatteryThreshold { get; set; }
        
        public bool? NeedsBatteryReplacement { get; set; }
        
        public bool? IsAvailable { get; set; }
        
        public string? VehicleType { get; set; }
    }

    public class TestEmailRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string TemporaryPassword { get; set; } = string.Empty;
    }
}
