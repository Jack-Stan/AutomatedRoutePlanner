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
        Task<bool> ValidateTokenAsync(string token);
        Task<UserDto?> GetUserFromTokenAsync(string token);
        Task<int> GetUsersCountAsync();
    }

    public interface IRouteOptimizationService
    {
        Task<RouteOptimizationResult> OptimizeRouteAsync(List<Vehicle> vehicles, int targetDurationMinutes);
        Task<List<int>> OptimizeRouteAsync(CreateRouteRequest request);
        Task<double> CalculateDistanceAsync(double lat1, double lon1, double lat2, double lon2);
        Task<int> CalculateTravelTimeAsync(double lat1, double lon1, double lat2, double lon2);
    }

    public interface IRouteService
    {
        Task<RouteGenerationResponse> GenerateRouteAsync(RouteGenerationRequest request);
        Task<List<RouteDto>> GetRouteSuggestionsAsync(int zoneId);
        Task<RouteDto?> GetRouteByIdAsync(int routeId);
        Task<RouteDto?> ConfirmRouteAsync(int routeId);
        Task<RouteDto?> GetTodaysRouteForSwapperAsync(int swapperId);
        Task<List<RouteDto>> GetRoutesByStatusAsync(Domain.Enums.RouteStatus status);
    }

    public interface IVehicleService
    {
        Task<List<VehicleDto>> GetLowBatteryVehiclesAsync(int zoneId, int batteryThreshold = 25);
        Task<List<VehicleDto>> GetVehiclesByZoneAsync(int zoneId);
        Task<VehicleDto?> GetVehicleByIdAsync(int vehicleId);
    }

    public interface IZoneService
    {
        Task<List<ZoneDto>> GetAllZonesAsync();
        Task<ZoneDto?> GetZoneByIdAsync(int zoneId);
    }

    public interface IRouteStopService
    {
        Task<bool> CompleteRouteStopAsync(int routeStopId);
        Task<List<RouteStopDto>> GetRouteStopsAsync(int routeId);
    }

    public interface IEmailService
    {
        Task<bool> SendTemporaryPasswordEmailAsync(string email, string firstName, string lastName, string username, string temporaryPassword);
        Task<bool> SendPasswordResetEmailAsync(string email, string firstName, string resetLink);
        Task<bool> SendWelcomeEmailAsync(string email, string firstName, string lastName);
    }
}
