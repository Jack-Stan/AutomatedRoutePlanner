using System.ComponentModel.DataAnnotations;

namespace HoppyRoute.Application.DTOs
{
    public class ParkingZoneDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double RadiusMeters { get; set; }
        public int MaxCapacity { get; set; }
        public int CurrentVehicleCount { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // Zone information
        public int ZoneId { get; set; }
        public string ZoneName { get; set; } = string.Empty;
        
        // Vehicle information
        public List<VehicleDto> Vehicles { get; set; } = new List<VehicleDto>();
        public int AvailableSpaces => MaxCapacity - CurrentVehicleCount;
        public bool IsFull => CurrentVehicleCount >= MaxCapacity;
    }

    public class CreateParkingZoneDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [Range(-90, 90)]
        public double Latitude { get; set; }

        [Required]
        [Range(-180, 180)]
        public double Longitude { get; set; }

        [Range(10, 500)]
        public double RadiusMeters { get; set; } = 50;

        [Range(1, 100)]
        public int MaxCapacity { get; set; } = 10;

        [Required]
        public int ZoneId { get; set; }
    }

    public class UpdateParkingZoneDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [Range(-90, 90)]
        public double Latitude { get; set; }

        [Required]
        [Range(-180, 180)]
        public double Longitude { get; set; }

        [Range(10, 500)]
        public double RadiusMeters { get; set; } = 50;

        [Range(1, 100)]
        public int MaxCapacity { get; set; } = 10;

        public bool IsActive { get; set; } = true;
    }
}
