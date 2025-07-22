using Microsoft.EntityFrameworkCore;
using HoppyRoute.Application.DTOs;
using HoppyRoute.Application.Interfaces;
using HoppyRoute.Domain.Entities;
using HoppyRoute.Domain.Enums;
using HoppyRoute.Infrastructure.Data;

namespace HoppyRoute.Application.Services
{
    public class RouteService : IRouteService
    {
        private readonly HoppyDbContext _context;
        private readonly IRouteOptimizationService _optimizationService;

        public RouteService(HoppyDbContext context, IRouteOptimizationService optimizationService)
        {
            _context = context;
            _optimizationService = optimizationService;
        }

        public async Task<RouteGenerationResponse> GenerateRouteAsync(RouteGenerationRequest request)
        {
            // Convert RouteGenerationRequest to CreateRouteRequest
            var createRequest = new CreateRouteRequest
            {
                AssignedSwapperId = request.AssignedSwapperId,
                ZoneId = request.ZoneId,
                Date = request.StartTime ?? DateTime.Today,
                TargetDurationMinutes = request.TargetDurationMinutes,
                VehicleIds = new List<int>() // Will be populated from low battery vehicles
            };

            // Get low battery vehicles in the zone
            var lowBatteryVehicles = await _context.Vehicles
                .Where(v => v.ZoneId == request.ZoneId && v.BatteryLevel <= (request.BatteryThreshold ?? 25))
                .Select(v => v.Id)
                .ToListAsync();

            createRequest.VehicleIds = lowBatteryVehicles;

            if (!createRequest.VehicleIds.Any())
            {
                return new RouteGenerationResponse
                {
                    Success = false,
                    Message = "No vehicles found with low battery in the specified zone"
                };
            }

            try
            {
                var route = await CreateOptimizedRouteAsync(createRequest);
                return new RouteGenerationResponse
                {
                    Success = true,
                    Message = "Route generated successfully",
                    Route = route,
                    TotalVehicles = createRequest.VehicleIds.Count,
                    EstimatedDurationMinutes = route.TargetDurationMinutes
                };
            }
            catch (Exception ex)
            {
                return new RouteGenerationResponse
                {
                    Success = false,
                    Message = $"Error generating route: {ex.Message}"
                };
            }
        }

        public async Task<List<RouteDto>> GetRouteSuggestionsAsync(int zoneId)
        {
            return await GetRoutesByZoneAsync(zoneId);
        }

        public async Task<RouteDto?> GetTodaysRouteForSwapperAsync(int swapperId)
        {
            var today = DateTime.Today;
            var route = await _context.Routes
                .Include(r => r.AssignedSwapper)
                .Include(r => r.Zone)
                .Include(r => r.Stops)
                    .ThenInclude(s => s.Vehicle)
                .FirstOrDefaultAsync(r => r.AssignedSwapperId == swapperId && r.Date.Date == today);

            return route != null ? MapToRouteDto(route) : null;
        }

        public async Task<RouteDto> CreateOptimizedRouteAsync(CreateRouteRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Create the route first
                var route = new Route
                {
                    AssignedSwapperId = request.AssignedSwapperId,
                    ZoneId = request.ZoneId,
                    Date = request.Date,
                    TargetDurationMinutes = request.TargetDurationMinutes,
                    Status = RouteStatus.Suggested,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Routes.Add(route);
                await _context.SaveChangesAsync();

                // Create unoptimized stops
                var stops = new List<RouteStop>();
                for (int i = 0; i < request.VehicleIds.Count; i++)
                {
                    var vehicleId = request.VehicleIds[i];
                    // Validate that vehicle exists and is in the specified zone
                    var vehicle = await _context.Vehicles
                        .FirstOrDefaultAsync(v => v.Id == vehicleId && v.ZoneId == request.ZoneId);

                    if (vehicle != null)
                    {
                        var stop = new RouteStop
                        {
                            RouteId = route.Id,
                            VehicleId = vehicleId,
                            Order = i + 1, // Temporary order, will be optimized
                            EstimatedArrivalOffset = TimeSpan.FromMinutes(i * 10), // Placeholder
                            EstimatedDurationAtStop = TimeSpan.FromMinutes(15), // 15 min per stop
                            Status = RouteStopStatus.Pending
                        };
                        stops.Add(stop);
                    }
                }

                // Get optimization
                var optimizedOrder = await _optimizationService.OptimizeRouteAsync(request);
                
                // Update stop orders based on optimization
                for (int i = 0; i < optimizedOrder.Count; i++)
                {
                    var stop = stops.FirstOrDefault(s => s.VehicleId == optimizedOrder[i]);
                    if (stop != null)
                    {
                        stop.Order = i + 1;
                        stop.EstimatedArrivalOffset = TimeSpan.FromMinutes(i * 15); // 15 min per stop
                    }
                }

                _context.RouteStops.AddRange(stops);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Reload route with all includes for return
                var createdRoute = await _context.Routes
                    .Include(r => r.AssignedSwapper)
                    .Include(r => r.Zone)
                    .Include(r => r.Stops)
                        .ThenInclude(s => s.Vehicle)
                    .FirstOrDefaultAsync(r => r.Id == route.Id);

                var routeDto = MapToRouteDto(createdRoute!);
                return routeDto;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<RouteDto>> GetRoutesByZoneAsync(int zoneId)
        {
            var routes = await _context.Routes
                .Include(r => r.AssignedSwapper)
                .Include(r => r.Zone)
                .Include(r => r.Stops)
                    .ThenInclude(s => s.Vehicle)
                .Where(r => r.ZoneId == zoneId && r.Status == RouteStatus.Suggested)
                .ToListAsync();

            return routes.Select(MapToRouteDto).ToList();
        }

        public async Task<RouteDto?> GetRouteByIdAsync(int routeId)
        {
            var route = await _context.Routes
                .Include(r => r.AssignedSwapper)
                .Include(r => r.Zone)
                .Include(r => r.Stops)
                    .ThenInclude(s => s.Vehicle)
                .FirstOrDefaultAsync(r => r.Id == routeId);

            return route != null ? MapToRouteDto(route) : null;
        }

        public async Task<RouteDto?> ConfirmRouteAsync(int routeId)
        {
            var route = await _context.Routes
                .Include(r => r.AssignedSwapper)
                .Include(r => r.Zone)
                .Include(r => r.Stops)
                    .ThenInclude(s => s.Vehicle)
                .FirstOrDefaultAsync(r => r.Id == routeId);

            if (route == null || route.Status != RouteStatus.Suggested)
                return null;

            route.Status = RouteStatus.Confirmed;
            route.ConfirmedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return MapToRouteDto(route);
        }

        public async Task<List<RouteDto>> GetRoutesByStatusAsync(RouteStatus status)
        {
            var routes = await _context.Routes
                .Include(r => r.AssignedSwapper)
                .Include(r => r.Zone)
                .Include(r => r.Stops)
                    .ThenInclude(s => s.Vehicle)
                .Where(r => r.Status == status)
                .ToListAsync();

            return routes.Select(MapToRouteDto).ToList();
        }

        private RouteDto MapToRouteDto(Route route)
        {
            return new RouteDto
            {
                Id = route.Id,
                AssignedSwapperId = route.AssignedSwapperId,
                AssignedSwapperName = route.AssignedSwapper?.FirstName + " " + route.AssignedSwapper?.LastName ?? "",
                ZoneId = route.ZoneId,
                ZoneName = route.Zone?.Name ?? "",
                Date = route.Date,
                TargetDurationMinutes = route.TargetDurationMinutes,
                Status = route.Status,
                CreatedAt = route.CreatedAt,
                ConfirmedAt = route.ConfirmedAt,
                Stops = route.Stops?.Select(s => new RouteStopDto
                {
                    Id = s.Id,
                    RouteId = s.RouteId,
                    VehicleId = s.VehicleId,
                    Vehicle = s.Vehicle != null ? new VehicleDto
                    {
                        Id = s.Vehicle.Id,
                        ExternalId = s.Vehicle.ExternalId,
                        ZoneId = s.Vehicle.ZoneId,
                        Latitude = s.Vehicle.Latitude,
                        Longitude = s.Vehicle.Longitude,
                        BatteryLevel = s.Vehicle.BatteryLevel,
                        LastUpdated = s.Vehicle.LastUpdated
                    } : new VehicleDto(),
                    Order = s.Order,
                    EstimatedArrivalOffset = s.EstimatedArrivalOffset,
                    EstimatedDurationAtStop = s.EstimatedDurationAtStop,
                    Status = s.Status,
                    ActualArrivalTime = s.ActualArrivalTime,
                    ActualDepartureTime = s.ActualDepartureTime
                }).OrderBy(s => s.Order).ToList() ?? new List<RouteStopDto>()
            };
        }

        public async Task<RouteDto> CreateRouteAsync(CreateRouteRequest request, int createdByUserId)
        {
            var route = new Route
            {
                Name = request.Name,
                Description = request.Description,
                AssignedSwapperId = request.AssignedSwapperId,
                CreatedByUserId = createdByUserId,
                ZoneId = request.ZoneId,
                RegionId = request.RegionId,
                Date = request.Date,
                TargetDurationMinutes = request.TargetDurationMinutes,
                TotalVehicleCount = request.VehicleIds.Count,
                Status = RouteStatus.Suggested,
                CreatedAt = DateTime.UtcNow
            };

            _context.Routes.Add(route);
            await _context.SaveChangesAsync();

            return MapToRouteDto(route);
        }

        public async Task<RouteDto?> UpdateRouteAsync(int routeId, UpdateRouteRequest request)
        {
            var route = await _context.Routes.FirstOrDefaultAsync(r => r.Id == routeId);
            if (route == null) return null;

            route.Name = request.Name;
            route.Description = request.Description;
            route.Date = request.Date;
            route.TargetDurationMinutes = request.TargetDurationMinutes;
            route.EstimatedDistanceKm = request.EstimatedDistanceKm;

            await _context.SaveChangesAsync();
            return MapToRouteDto(route);
        }

        public async Task<bool> DeleteRouteAsync(int routeId)
        {
            var route = await _context.Routes.FirstOrDefaultAsync(r => r.Id == routeId);
            if (route == null) return false;

            _context.Routes.Remove(route);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<RouteDto>> GetRoutesByRegionAsync(int regionId)
        {
            var routes = await _context.Routes
                .Include(r => r.AssignedSwapper)
                .Include(r => r.Zone)
                .Where(r => r.RegionId == regionId)
                .ToListAsync();

            return routes.Select(MapToRouteDto).ToList();
        }

        public async Task<List<RouteDto>> GetRoutesByUserAsync(int userId)
        {
            var routes = await _context.Routes
                .Include(r => r.AssignedSwapper)
                .Include(r => r.Zone)
                .Where(r => r.AssignedSwapperId == userId || r.CreatedByUserId == userId)
                .ToListAsync();

            return routes.Select(MapToRouteDto).ToList();
        }

        public async Task<RouteDto?> StartRouteAsync(int routeId)
        {
            var route = await _context.Routes.FirstOrDefaultAsync(r => r.Id == routeId);
            if (route == null) return null;

            route.Status = RouteStatus.InProgress;
            route.StartedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return MapToRouteDto(route);
        }

        public async Task<RouteDto?> CompleteRouteAsync(int routeId)
        {
            var route = await _context.Routes.FirstOrDefaultAsync(r => r.Id == routeId);
            if (route == null) return null;

            route.Status = RouteStatus.Completed;
            route.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return MapToRouteDto(route);
        }

        public async Task<List<RouteDto>> GetActiveRoutesAsync()
        {
            var routes = await _context.Routes
                .Include(r => r.AssignedSwapper)
                .Include(r => r.Zone)
                .Where(r => r.Status == RouteStatus.InProgress || r.Status == RouteStatus.Confirmed)
                .ToListAsync();

            return routes.Select(MapToRouteDto).ToList();
        }
    }
}
