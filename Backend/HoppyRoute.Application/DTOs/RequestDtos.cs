namespace HoppyRoute.Application.DTOs
{
    public class RouteGenerationRequest
    {
        public int SwapperId { get; set; }
        public int ZoneId { get; set; }
        public int TargetDurationMinutes { get; set; }
        public int? BatteryThreshold { get; set; } = 25; // Default 25%
        public DateTime? StartTime { get; set; }
    }

    public class RouteGenerationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public RouteDto? Route { get; set; }
        public int TotalVehicles { get; set; }
        public int EstimatedDurationMinutes { get; set; }
        public double TotalDistanceKm { get; set; }
    }

    public class LowBatteryVehiclesRequest
    {
        public int ZoneId { get; set; }
        public int BatteryThreshold { get; set; } = 25;
    }

    public class RouteOptimizationResult
    {
        public List<int> OptimalOrder { get; set; } = new List<int>();
        public int TotalDurationMinutes { get; set; }
        public double TotalDistanceKm { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class CreateRouteRequest
    {
        public int SwapperId { get; set; }
        public int ZoneId { get; set; }
        public DateTime Date { get; set; }
        public int TargetDurationMinutes { get; set; }
        public List<int> VehicleIds { get; set; } = new List<int>();
    }
}
