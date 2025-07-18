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
        
        [Required]
        [Range(-90, 90)]
        public double Latitude { get; set; }
        
        [Required]
        [Range(-180, 180)]
        public double Longitude { get; set; }
        
        [Required]
        [Range(0, 100)]
        public int BatteryLevel { get; set; }
        
        [Required]
        public DateTime LastUpdated { get; set; }
        
        // Navigation properties
        [ForeignKey("ZoneId")]
        public virtual Zone? Zone { get; set; }
        
        public virtual ICollection<RouteStop> RouteStops { get; set; } = new List<RouteStop>();
    }
}
