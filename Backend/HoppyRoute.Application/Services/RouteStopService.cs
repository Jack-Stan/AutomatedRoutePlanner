using Microsoft.EntityFrameworkCore;
using HoppyRoute.Application.DTOs;
using HoppyRoute.Application.Interfaces;
using HoppyRoute.Domain.Enums;
using HoppyRoute.Infrastructure.Data;

namespace HoppyRoute.Application.Services
{
    public class RouteStopService : IRouteStopService
    {
        private readonly HoppyDbContext _context;

        public RouteStopService(HoppyDbContext context)
        {
            _context = context;
        }

        public async Task<bool> CompleteRouteStopAsync(int routeStopId)
        {
            var routeStop = await _context.RouteStops.FindAsync(routeStopId);

            if (routeStop == null || routeStop.Status != RouteStopStatus.Pending)
                return false;

            routeStop.Status = RouteStopStatus.Completed;
            routeStop.ActualArrivalTime = DateTime.UtcNow;
            routeStop.ActualDepartureTime = DateTime.UtcNow.AddMinutes(5); // Assume 5 minutes for battery swap

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<RouteStopDto>> GetRouteStopsAsync(int routeId)
        {
            var stops = await _context.RouteStops
                .Include(s => s.Vehicle)
                    .ThenInclude(v => v.Zone)
                .Where(s => s.RouteId == routeId)
                .OrderBy(s => s.Order)
                .ToListAsync();

            return stops.Select(s => new RouteStopDto
            {
                Id = s.Id,
                RouteId = s.RouteId,
                VehicleId = s.VehicleId,
                Order = s.Order,
                EstimatedArrivalOffset = s.EstimatedArrivalOffset,
                EstimatedDurationAtStop = s.EstimatedDurationAtStop,
                Status = s.Status,
                ActualArrivalTime = s.ActualArrivalTime,
                ActualDepartureTime = s.ActualDepartureTime,
                Vehicle = new VehicleDto
                {
                    Id = s.Vehicle.Id,
                    ExternalId = s.Vehicle.ExternalId,
                    ZoneId = s.Vehicle.ZoneId,
                    ZoneName = s.Vehicle.Zone?.Name,
                    Latitude = s.Vehicle.Latitude,
                    Longitude = s.Vehicle.Longitude,
                    BatteryLevel = s.Vehicle.BatteryLevel,
                    LastUpdated = s.Vehicle.LastUpdated
                }
            }).ToList();
        }
    }
}
