using Microsoft.EntityFrameworkCore;
using HoppyRoute.Application.DTOs;
using HoppyRoute.Application.Interfaces;
using HoppyRoute.Infrastructure.Data;

namespace HoppyRoute.Application.Services
{
    public class VehicleService : IVehicleService
    {
        private readonly HoppyDbContext _context;

        public VehicleService(HoppyDbContext context)
        {
            _context = context;
        }

        public async Task<List<VehicleDto>> GetLowBatteryVehiclesAsync(int zoneId, int batteryThreshold = 25)
        {
            var vehicles = await _context.Vehicles
                .Include(v => v.Zone)
                .Where(v => v.ZoneId == zoneId && v.BatteryLevel <= batteryThreshold)
                .OrderBy(v => v.BatteryLevel)
                .ToListAsync();

            return vehicles.Select(v => new VehicleDto
            {
                Id = v.Id,
                ExternalId = v.ExternalId,
                ZoneId = v.ZoneId,
                ZoneName = v.Zone?.Name,
                Latitude = v.Latitude,
                Longitude = v.Longitude,
                BatteryLevel = v.BatteryLevel,
                LastUpdated = v.LastUpdated
            }).ToList();
        }

        public async Task<List<VehicleDto>> GetVehiclesByZoneAsync(int zoneId)
        {
            var vehicles = await _context.Vehicles
                .Include(v => v.Zone)
                .Where(v => v.ZoneId == zoneId)
                .OrderBy(v => v.ExternalId)
                .ToListAsync();

            return vehicles.Select(v => new VehicleDto
            {
                Id = v.Id,
                ExternalId = v.ExternalId,
                ZoneId = v.ZoneId,
                ZoneName = v.Zone?.Name,
                Latitude = v.Latitude,
                Longitude = v.Longitude,
                BatteryLevel = v.BatteryLevel,
                LastUpdated = v.LastUpdated
            }).ToList();
        }

        public async Task<VehicleDto?> GetVehicleByIdAsync(int vehicleId)
        {
            var vehicle = await _context.Vehicles
                .Include(v => v.Zone)
                .FirstOrDefaultAsync(v => v.Id == vehicleId);

            if (vehicle == null)
                return null;

            return new VehicleDto
            {
                Id = vehicle.Id,
                ExternalId = vehicle.ExternalId,
                ZoneId = vehicle.ZoneId,
                ZoneName = vehicle.Zone?.Name,
                Latitude = vehicle.Latitude,
                Longitude = vehicle.Longitude,
                BatteryLevel = vehicle.BatteryLevel,
                LastUpdated = vehicle.LastUpdated
            };
        }
    }
}
