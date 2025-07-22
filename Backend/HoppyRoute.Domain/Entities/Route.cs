using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HoppyRoute.Domain.Enums;

namespace HoppyRoute.Domain.Entities
{
    public class Route
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        public int AssignedSwapperId { get; set; } // User with role BatterySwapper
        
        [Required]
        public int CreatedByUserId { get; set; } // User with role FleetManager
        
        [Required]
        public int ZoneId { get; set; }
        
        public int? RegionId { get; set; } // Optional direct region link
        
        [Required]
        public DateTime Date { get; set; }
        
        [Required]
        [Range(60, 1440)] // 1 hour to 24 hours in minutes
        public int TargetDurationMinutes { get; set; }
        
        // Estimated distance in kilometers
        public double? EstimatedDistanceKm { get; set; }
        
        // Total number of vehicles to process
        public int TotalVehicleCount { get; set; } = 0;
        
        [Required]
        public RouteStatus Status { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; }
        
        public DateTime? ConfirmedAt { get; set; }
        
        public DateTime? StartedAt { get; set; }
        
        public DateTime? CompletedAt { get; set; }
        
        // Navigation properties
        [ForeignKey("AssignedSwapperId")]
        public virtual User AssignedSwapper { get; set; } = null!;
        
        [ForeignKey("CreatedByUserId")]
        public virtual User CreatedByUser { get; set; } = null!;
        
        [ForeignKey("ZoneId")]
        public virtual Zone Zone { get; set; } = null!;
        
        [ForeignKey("RegionId")]
        public virtual Region? Region { get; set; }
        
        public virtual ICollection<RouteStop> Stops { get; set; } = new List<RouteStop>();
    }
}
