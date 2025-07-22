using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HoppyRoute.Domain.Entities
{
    public class Vehicle
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string ExternalId { get; set; } = string.Empty;
        
        [Required]
        [StringLength(20)]
        public string RegistrationNumber { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string VehicleType { get; set; } = string.Empty; // E-Scooter, E-Bike, etc.
        
        public int? ZoneId { get; set; }
        
        // Current parking zone where the vehicle is located
        public int? CurrentParkingZoneId { get; set; }
        
        [Required]
        [Range(-90, 90)]
        public double Latitude { get; set; }
        
        [Required]
        [Range(-180, 180)]
        public double Longitude { get; set; }
        
        [Required]
        [Range(0, 100)]
        public int BatteryLevel { get; set; }
        
        // Indicates if battery needs replacement (usually < 20%)
        public bool NeedsBatteryReplacement { get; set; } = false;
        
        // Indicates if vehicle is available for route planning
        public bool IsAvailable { get; set; } = true;
        
        [Required]
        public DateTime LastUpdated { get; set; }
        
        // Navigation properties
        [ForeignKey("ZoneId")]
        public virtual Zone? Zone { get; set; }
        
        [ForeignKey("CurrentParkingZoneId")]
        public virtual ParkingZone? CurrentParkingZone { get; set; }
        
        public virtual ICollection<RouteStop> RouteStops { get; set; } = new List<RouteStop>();
    }
}
