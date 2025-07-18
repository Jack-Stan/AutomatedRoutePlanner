using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HoppyRoute.Domain.Enums;

namespace HoppyRoute.Domain.Entities
{
    public class Route
    {
        public int Id { get; set; }
        
        [Required]
        public int SwapperId { get; set; }
        
        [Required]
        public int ZoneId { get; set; }
        
        [Required]
        public DateTime Date { get; set; }
        
        [Required]
        [Range(60, 1440)] // 1 hour to 24 hours in minutes
        public int TargetDurationMinutes { get; set; }
        
        [Required]
        public RouteStatus Status { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; }
        
        public DateTime? ConfirmedAt { get; set; }
        
        // Navigation properties
        [ForeignKey("SwapperId")]
        public virtual Swapper Swapper { get; set; } = null!;
        
        [ForeignKey("ZoneId")]
        public virtual Zone Zone { get; set; } = null!;
        
        public virtual ICollection<RouteStop> Stops { get; set; } = new List<RouteStop>();
    }
}
