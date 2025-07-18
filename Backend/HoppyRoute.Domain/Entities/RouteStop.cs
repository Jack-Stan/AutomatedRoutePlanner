using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HoppyRoute.Domain.Enums;

namespace HoppyRoute.Domain.Entities
{
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
        public TimeSpan EstimatedArrivalOffset { get; set; }
        
        [Required]
        public TimeSpan EstimatedDurationAtStop { get; set; }
        
        [Required]
        public RouteStopStatus Status { get; set; }
        
        public DateTime? ActualArrivalTime { get; set; }
        
        public DateTime? ActualDepartureTime { get; set; }
        
        // Navigation properties
        [ForeignKey("RouteId")]
        public virtual Route Route { get; set; } = null!;
        
        [ForeignKey("VehicleId")]
        public virtual Vehicle Vehicle { get; set; } = null!;
    }
}
