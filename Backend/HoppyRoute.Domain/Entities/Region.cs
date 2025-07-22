using System.ComponentModel.DataAnnotations;

namespace HoppyRoute.Domain.Entities
{
    public class Region
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Country { get; set; } = string.Empty;

        [Required]
        [StringLength(3)]
        public string CountryCode { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // GPS coordinates for region center (optional, for mapping)
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // Navigation properties
        public virtual ICollection<Zone> Zones { get; set; } = new List<Zone>();
        public virtual ICollection<User> AssignedUsers { get; set; } = new List<User>();
        public virtual ICollection<Route> Routes { get; set; } = new List<Route>();
    }
}
