using System.ComponentModel.DataAnnotations;

namespace HoppyRoute.Application.DTOs
{
    public class RegionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        
        // Navigation properties
        public List<ZoneDto> Zones { get; set; } = new List<ZoneDto>();
        public int TotalZones { get; set; }
        public int TotalVehicles { get; set; }
        public int TotalUsers { get; set; }
    }

    public class CreateRegionDto
    {
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

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public class UpdateRegionDto
    {
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

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public class CountryDto
    {
        public string CountryCode { get; set; } = string.Empty;
        public string CountryName { get; set; } = string.Empty;
        public List<RegionDto> Regions { get; set; } = new List<RegionDto>();
    }
}
