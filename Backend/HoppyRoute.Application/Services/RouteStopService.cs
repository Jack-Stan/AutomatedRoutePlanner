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
                    RegistrationNumber = s.Vehicle.RegistrationNumber,
                    VehicleType = s.Vehicle.VehicleType,
                    ZoneId = s.Vehicle.ZoneId,
                    ZoneName = s.Vehicle.Zone?.Name,
                    Latitude = s.Vehicle.Latitude,
                    Longitude = s.Vehicle.Longitude,
                    BatteryLevel = s.Vehicle.BatteryLevel,
                    NeedsBatteryReplacement = s.Vehicle.NeedsBatteryReplacement,
                    IsAvailable = s.Vehicle.IsAvailable,
                    LastUpdated = s.Vehicle.LastUpdated
                }
            }).ToList();
        }

        public async Task<RouteStopDto?> UpdateRouteStopStatusAsync(int routeStopId, RouteStopStatus status)
        {
            var routeStop = await _context.RouteStops
                .Include(s => s.Vehicle)
                .ThenInclude(v => v.Zone)
                .FirstOrDefaultAsync(s => s.Id == routeStopId);

            if (routeStop == null) return null;

            routeStop.Status = status;
            if (status == RouteStopStatus.InProgress)
            {
                routeStop.ActualArrivalTime = DateTime.UtcNow;
            }
            else if (status == RouteStopStatus.Completed)
            {
                routeStop.ActualDepartureTime = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return MapToRouteStopDto(routeStop);
        }

        public async Task<RouteStopDto?> GetRouteStopByIdAsync(int routeStopId)
        {
            var routeStop = await _context.RouteStops
                .Include(s => s.Vehicle)
                .ThenInclude(v => v.Zone)
                .Include(s => s.PickupParkingZone)
                .Include(s => s.DropoffParkingZone)
                .FirstOrDefaultAsync(s => s.Id == routeStopId);

            return routeStop != null ? MapToRouteStopDto(routeStop) : null;
        }

        public async Task<bool> AddNotesToRouteStopAsync(int routeStopId, string notes)
        {
            var routeStop = await _context.RouteStops.FirstOrDefaultAsync(s => s.Id == routeStopId);
            if (routeStop == null) return false;

            routeStop.Notes = notes;
            await _context.SaveChangesAsync();
            return true;
        }

        private RouteStopDto MapToRouteStopDto(Domain.Entities.RouteStop routeStop)
        {
            return new RouteStopDto
            {
                Id = routeStop.Id,
                RouteId = routeStop.RouteId,
                VehicleId = routeStop.VehicleId,
                Order = routeStop.Order,
                StopType = routeStop.StopType,
                PickupParkingZoneId = routeStop.PickupParkingZoneId,
                PickupParkingZoneName = routeStop.PickupParkingZone?.Name,
                DropoffParkingZoneId = routeStop.DropoffParkingZoneId,
                DropoffParkingZoneName = routeStop.DropoffParkingZone?.Name,
                EstimatedArrivalOffset = routeStop.EstimatedArrivalOffset,
                EstimatedDurationAtStop = routeStop.EstimatedDurationAtStop,
                Status = routeStop.Status,
                ActualArrivalTime = routeStop.ActualArrivalTime,
                ActualDepartureTime = routeStop.ActualDepartureTime,
                Notes = routeStop.Notes,
                Latitude = routeStop.Latitude,
                Longitude = routeStop.Longitude,
                Vehicle = new VehicleDto
                {
                    Id = routeStop.Vehicle.Id,
                    ExternalId = routeStop.Vehicle.ExternalId,
                    RegistrationNumber = routeStop.Vehicle.RegistrationNumber,
                    VehicleType = routeStop.Vehicle.VehicleType,
                    ZoneId = routeStop.Vehicle.ZoneId,
                    ZoneName = routeStop.Vehicle.Zone?.Name,
                    Latitude = routeStop.Vehicle.Latitude,
                    Longitude = routeStop.Vehicle.Longitude,
                    BatteryLevel = routeStop.Vehicle.BatteryLevel,
                    NeedsBatteryReplacement = routeStop.Vehicle.NeedsBatteryReplacement,
                    IsAvailable = routeStop.Vehicle.IsAvailable,
                    LastUpdated = routeStop.Vehicle.LastUpdated
                }
            };
        }
    }
}
