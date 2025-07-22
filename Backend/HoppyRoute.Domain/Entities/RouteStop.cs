using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HoppyRoute.Domain.Enums;

namespace HoppyRoute.Domain.Entities
{
    public enum RouteStopType
    {
        Pickup = 1,
        Dropoff = 2
    }

    public class RouteStop
    {
        public int Id { get; set; }
        
        [Required]
        public int RouteId { get; set; }
        
        [Required]
        public int VehicleId { get; set; }
        
        [Required]
        [Range(1, int.MaxValue)]
        public int Order { get; set; }
        
        [Required]
        public RouteStopType StopType { get; set; }
        
        // Parking zone where vehicle is picked up from
        public int? PickupParkingZoneId { get; set; }
        
        // Parking zone where vehicle is dropped off to
        public int? DropoffParkingZoneId { get; set; }
        
        [Required]
        public TimeSpan EstimatedArrivalOffset { get; set; }
        
        [Required]
        public TimeSpan EstimatedDurationAtStop { get; set; }
        
        [Required]
        public RouteStopStatus Status { get; set; }
        
        public DateTime? ActualArrivalTime { get; set; }
        
        public DateTime? ActualDepartureTime { get; set; }
        
        [StringLength(500)]
        public string? Notes { get; set; }
        
        // GPS coordinates for the stop
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        
        // Navigation properties
        [ForeignKey("RouteId")]
        public virtual Route Route { get; set; } = null!;
        
        [ForeignKey("VehicleId")]
        public virtual Vehicle Vehicle { get; set; } = null!;
        
        [ForeignKey("PickupParkingZoneId")]
        public virtual ParkingZone? PickupParkingZone { get; set; }
        
        [ForeignKey("DropoffParkingZoneId")]
        public virtual ParkingZone? DropoffParkingZone { get; set; }
    }
}
