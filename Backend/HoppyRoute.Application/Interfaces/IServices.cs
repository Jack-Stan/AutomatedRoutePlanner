using HoppyRoute.Application.DTOs;
using HoppyRoute.Domain.Entities;
using HoppyRoute.Domain.Enums;

namespace HoppyRoute.Application.Interfaces
{
    // Authentication & User Management
    public interface IAuthService
    {
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
        Task<UserDto> CreateUserAsync(CreateUserRequestDto request, int createdByUserId);
        Task<UserDto> GetUserByIdAsync(int userId);
        Task<IEnumerable<UserDto>> GetUsersByRoleAsync(UserRole role, int? createdByUserId = null);
        Task<UserDto> UpdateUserAsync(int userId, UpdateUserRequestDto request);
        Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequestDto request);
        Task<bool> DeactivateUserAsync(int userId);
        Task<(bool Success, string TemporaryPassword)> ResetUserPasswordAsync(int userId);
        Task<bool> InitiatePasswordResetAsync(string email);
        Task<PasswordResetResult> ResetPasswordWithTokenAsync(string token, string newPassword);
        Task<bool> ValidateTokenAsync(string token);
        Task<UserDto?> GetUserFromTokenAsync(string token);
        Task<int> GetUsersCountAsync();
        Task<bool> TestEmailAsync(string email, string firstName, string lastName, string username, string temporaryPassword);
        Task<bool> AssignUserToRegionAsync(int userId, int regionId);
        Task<List<UserDto>> GetUsersByRegionAsync(int regionId);
    }

    // Region Management
    public interface IRegionService
    {
        Task<List<RegionDto>> GetAllRegionsAsync();
        Task<RegionDto?> GetRegionByIdAsync(int regionId);
        Task<RegionDto> CreateRegionAsync(CreateRegionDto request);
        Task<RegionDto?> UpdateRegionAsync(int regionId, UpdateRegionDto request);
        Task<bool> DeleteRegionAsync(int regionId);
        Task<List<ZoneDto>> GetZonesByRegionAsync(int regionId);
        Task<List<VehicleDto>> GetVehiclesByRegionAsync(GetVehiclesInRegionRequest request);
        Task<List<UserDto>> GetUsersByRegionAsync(int regionId);
        
        // New methods for country/region structure
        Task<List<CountryDto>> GetCountriesAsync();
        Task<List<RegionDto>> GetRegionsByCountryAsync(string countryCode);
    }

    // Parking Zone Management
    public interface IParkingZoneService
    {
        Task<List<ParkingZoneDto>> GetAllParkingZonesAsync();
        Task<List<ParkingZoneDto>> GetParkingZonesByZoneAsync(int zoneId);
        Task<ParkingZoneDto?> GetParkingZoneByIdAsync(int parkingZoneId);
        Task<ParkingZoneDto> CreateParkingZoneAsync(CreateParkingZoneDto request);
        Task<ParkingZoneDto?> UpdateParkingZoneAsync(int parkingZoneId, UpdateParkingZoneDto request);
        Task<bool> DeleteParkingZoneAsync(int parkingZoneId);
        Task<List<VehicleDto>> GetVehiclesInParkingZoneAsync(int parkingZoneId);
        Task<bool> UpdateVehicleCountAsync(int parkingZoneId);
    }

    public interface IRouteOptimizationService
    {
        Task<RouteOptimizationResult> OptimizeRouteAsync(List<Vehicle> vehicles, int targetDurationMinutes);
        Task<List<int>> OptimizeRouteAsync(CreateRouteRequest request);
        Task<double> CalculateDistanceAsync(double lat1, double lon1, double lat2, double lon2);
        Task<int> CalculateTravelTimeAsync(double lat1, double lon1, double lat2, double lon2);
        Task<RouteOptimizationResult> OptimizeRouteWithParkingZonesAsync(List<Vehicle> vehicles, List<ParkingZone> parkingZones, int targetDurationMinutes);
    }

    public interface IRouteService
    {
        Task<RouteGenerationResponse> GenerateRouteAsync(RouteGenerationRequest request);
        Task<RouteDto> CreateRouteAsync(CreateRouteRequest request, int createdByUserId);
        Task<RouteDto?> UpdateRouteAsync(int routeId, UpdateRouteRequest request);
        Task<bool> DeleteRouteAsync(int routeId);
        Task<List<RouteDto>> GetRouteSuggestionsAsync(int zoneId);
        Task<List<RouteDto>> GetRoutesByRegionAsync(int regionId);
        Task<List<RouteDto>> GetRoutesByUserAsync(int userId);
        Task<RouteDto?> GetRouteByIdAsync(int routeId);
        Task<RouteDto?> ConfirmRouteAsync(int routeId);
        Task<RouteDto?> StartRouteAsync(int routeId);
        Task<RouteDto?> CompleteRouteAsync(int routeId);
        Task<RouteDto?> GetTodaysRouteForSwapperAsync(int swapperId);
        Task<List<RouteDto>> GetRoutesByStatusAsync(Domain.Enums.RouteStatus status);
        Task<List<RouteDto>> GetActiveRoutesAsync();
        
        // Route Approval Methods
        Task<RouteDto?> ApproveRouteAsync(int routeId, int approvedBy, string? notes = null);
        Task<RouteDto?> RejectRouteAsync(int routeId, int approvedBy, string? notes = null);
        Task<List<RouteDto>> GetRoutesPendingApprovalAsync(int? zoneId = null);
    }

    public interface IVehicleService
    {
        Task<List<VehicleDto>> GetLowBatteryVehiclesAsync(int zoneId, int batteryThreshold = 25);
        Task<List<VehicleDto>> GetAllLowBatteryVehiclesAsync(int batteryThreshold = 25);
        Task<List<VehicleDto>> GetVehiclesByZoneAsync(int zoneId);
        Task<List<VehicleDto>> GetAllVehiclesAsync();
        Task<List<VehicleDto>> GetVehiclesByRegionAsync(int regionId);
        Task<List<VehicleDto>> GetVehiclesByParkingZoneAsync(int parkingZoneId);
        Task<VehicleDto?> GetVehicleByIdAsync(int vehicleId);
        Task<VehicleDto?> UpdateVehicleLocationAsync(int vehicleId, double latitude, double longitude);
        Task<VehicleDto?> UpdateVehicleBatteryAsync(int vehicleId, int batteryLevel);
        Task<bool> MoveVehicleToParkingZoneAsync(int vehicleId, int? parkingZoneId);
        Task<List<VehicleDto>> GetAvailableVehiclesAsync(int zoneId);
        Task<List<VehicleDto>> GetVehiclesNeedingBatteryReplacementAsync(int zoneId);
    }

    public interface IZoneService
    {
        Task<List<ZoneDto>> GetAllZonesAsync();
        Task<List<ZoneDto>> GetZonesByRegionAsync(int regionId);
        Task<ZoneDto?> GetZoneByIdAsync(int zoneId);
        Task<List<ParkingZoneDto>> GetParkingZonesByZoneAsync(int zoneId);
    }

    public interface IRouteStopService
    {
        Task<bool> CompleteRouteStopAsync(int routeStopId);
        Task<RouteStopDto?> UpdateRouteStopStatusAsync(int routeStopId, RouteStopStatus status);
        Task<List<RouteStopDto>> GetRouteStopsAsync(int routeId);
        Task<RouteStopDto?> GetRouteStopByIdAsync(int routeStopId);
        Task<bool> AddNotesToRouteStopAsync(int routeStopId, string notes);
    }

    public interface IEmailService
    {
        Task<bool> SendTemporaryPasswordEmailAsync(string email, string firstName, string lastName, string username, string temporaryPassword);
        Task<bool> SendPasswordResetEmailAsync(string email, string firstName, string resetLink);
        Task<bool> SendWelcomeEmailAsync(string email, string firstName, string lastName);
    }
}
