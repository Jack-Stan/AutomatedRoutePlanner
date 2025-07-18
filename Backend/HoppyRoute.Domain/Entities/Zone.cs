using System.ComponentModel.DataAnnotations;

namespace HoppyRoute.Domain.Entities
{
    public class Zone
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [StringLength(3)]
        public string CountryCode { get; set; } = string.Empty;
        
        [Required]
        public string GeoJsonBoundary { get; set; } = string.Empty;
        
        // Navigation properties
        public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
        public virtual ICollection<Route> Routes { get; set; } = new List<Route>();
    }
}
