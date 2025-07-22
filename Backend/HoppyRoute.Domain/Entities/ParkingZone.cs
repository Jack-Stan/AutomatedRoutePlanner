using System.ComponentModel.DataAnnotations;

namespace HoppyRoute.Domain.Entities
{
    public class ParkingZone
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        // Radius in meters for the parking zone
        public double RadiusMeters { get; set; } = 50;

        // Maximum capacity of vehicles for this parking zone
        public int MaxCapacity { get; set; } = 10;

        // Current number of vehicles in this zone
        public int CurrentVehicleCount { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign keys
        public int ZoneId { get; set; }
        public Zone Zone { get; set; } = null!;

        // Navigation properties
        public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
        public virtual ICollection<RouteStop> PickupStops { get; set; } = new List<RouteStop>();
        public virtual ICollection<RouteStop> DropoffStops { get; set; } = new List<RouteStop>();
    }
}
