using HoppyRoute.Domain.Enums;

namespace HoppyRoute.Application.DTOs
{
    public class VehicleDto
    {
        public int Id { get; set; }
        public string ExternalId { get; set; } = string.Empty;
        public int? ZoneId { get; set; }
        public string? ZoneName { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int BatteryLevel { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class ZoneDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public string GeoJsonBoundary { get; set; } = string.Empty;
    }

    public class SwapperDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
    }

    public class RouteDto
    {
        public int Id { get; set; }
        public int SwapperId { get; set; }
        public string SwapperName { get; set; } = string.Empty;
        public int ZoneId { get; set; }
        public string ZoneName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int TargetDurationMinutes { get; set; }
        public RouteStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public List<RouteStopDto> Stops { get; set; } = new List<RouteStopDto>();
    }

    public class RouteStopDto
    {
        public int Id { get; set; }
        public int RouteId { get; set; }
        public int VehicleId { get; set; }
        public VehicleDto Vehicle { get; set; } = null!;
        public int Order { get; set; }
        public TimeSpan EstimatedArrivalOffset { get; set; }
        public TimeSpan EstimatedDurationAtStop { get; set; }
        public RouteStopStatus Status { get; set; }
        public DateTime? ActualArrivalTime { get; set; }
        public DateTime? ActualDepartureTime { get; set; }
    }
}
