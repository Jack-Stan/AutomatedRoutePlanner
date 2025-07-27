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
                .Where(v => v.ZoneId == zoneId && 
                           v.BatteryLevel <= batteryThreshold && 
                           v.IsAvailable == true)
                .OrderBy(v => v.BatteryLevel)
                .ToListAsync();

            return vehicles.Select(MapToVehicleDto).ToList();
        }

        public async Task<List<VehicleDto>> GetAllLowBatteryVehiclesAsync(int batteryThreshold = 25)
        {
            var vehicles = await _context.Vehicles
                .Include(v => v.Zone)
                .Where(v => v.BatteryLevel <= batteryThreshold && 
                           v.IsAvailable == true)
                .OrderBy(v => v.BatteryLevel)
                .ThenBy(v => v.ZoneId)
                .ToListAsync();

            return vehicles.Select(MapToVehicleDto).ToList();
        }

        public async Task<List<VehicleDto>> GetVehiclesByZoneAsync(int zoneId)
        {
            var vehicles = await _context.Vehicles
                .Include(v => v.Zone)
                .Where(v => v.ZoneId == zoneId)
                .OrderBy(v => v.ExternalId)
                .ToListAsync();

            return vehicles.Select(MapToVehicleDto).ToList();
        }

        public async Task<List<VehicleDto>> GetAllVehiclesAsync()
        {
            var vehicles = await _context.Vehicles
                .Include(v => v.Zone)
                .OrderBy(v => v.ZoneId)
                .ThenBy(v => v.ExternalId)
                .ToListAsync();

            return vehicles.Select(MapToVehicleDto).ToList();
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
                RegistrationNumber = vehicle.RegistrationNumber,
                VehicleType = vehicle.VehicleType,
                ZoneId = vehicle.ZoneId,
                ZoneName = vehicle.Zone?.Name,
                CurrentParkingZoneId = vehicle.CurrentParkingZoneId,
                Latitude = vehicle.Latitude,
                Longitude = vehicle.Longitude,
                BatteryLevel = vehicle.BatteryLevel,
                NeedsBatteryReplacement = vehicle.NeedsBatteryReplacement,
                IsAvailable = vehicle.IsAvailable,
                LastUpdated = vehicle.LastUpdated
            };
        }

        public async Task<List<VehicleDto>> GetVehiclesByRegionAsync(int regionId)
        {
            var vehicles = await _context.Vehicles
                .Include(v => v.Zone)
                .ThenInclude(z => z!.Region)
                .Where(v => v.Zone != null && v.Zone.RegionId == regionId)
                .OrderBy(v => v.ExternalId)
                .ToListAsync();

            return vehicles.Select(MapToVehicleDto).ToList();
        }

        public async Task<List<VehicleDto>> GetVehiclesByParkingZoneAsync(int parkingZoneId)
        {
            var vehicles = await _context.Vehicles
                .Include(v => v.Zone)
                .Include(v => v.CurrentParkingZone)
                .Where(v => v.CurrentParkingZoneId == parkingZoneId)
                .OrderBy(v => v.ExternalId)
                .ToListAsync();

            return vehicles.Select(MapToVehicleDto).ToList();
        }

        public async Task<VehicleDto?> UpdateVehicleLocationAsync(int vehicleId, double latitude, double longitude)
        {
            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId);
            if (vehicle == null) return null;

            vehicle.Latitude = latitude;
            vehicle.Longitude = longitude;
            vehicle.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapToVehicleDto(vehicle);
        }

        public async Task<VehicleDto?> UpdateVehicleBatteryAsync(int vehicleId, int batteryLevel)
        {
            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId);
            if (vehicle == null) return null;

            vehicle.BatteryLevel = batteryLevel;
            vehicle.NeedsBatteryReplacement = batteryLevel <= 25;
            vehicle.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapToVehicleDto(vehicle);
        }

        public async Task<bool> MoveVehicleToParkingZoneAsync(int vehicleId, int? parkingZoneId)
        {
            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId);
            if (vehicle == null) return false;

            // Update old parking zone count
            if (vehicle.CurrentParkingZoneId.HasValue)
            {
                var oldParkingZone = await _context.ParkingZones.FirstOrDefaultAsync(pz => pz.Id == vehicle.CurrentParkingZoneId);
                if (oldParkingZone != null && oldParkingZone.CurrentVehicleCount > 0)
                {
                    oldParkingZone.CurrentVehicleCount--;
                }
            }

            // Update new parking zone count
            if (parkingZoneId.HasValue)
            {
                var newParkingZone = await _context.ParkingZones.FirstOrDefaultAsync(pz => pz.Id == parkingZoneId);
                if (newParkingZone != null)
                {
                    newParkingZone.CurrentVehicleCount++;
                }
            }

            vehicle.CurrentParkingZoneId = parkingZoneId;
            vehicle.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<VehicleDto>> GetAvailableVehiclesAsync(int zoneId)
        {
            var vehicles = await _context.Vehicles
                .Include(v => v.Zone)
                .Where(v => v.ZoneId == zoneId && v.IsAvailable)
                .OrderBy(v => v.ExternalId)
                .ToListAsync();

            return vehicles.Select(MapToVehicleDto).ToList();
        }

        public async Task<List<VehicleDto>> GetVehiclesNeedingBatteryReplacementAsync(int zoneId)
        {
            var vehicles = await _context.Vehicles
                .Include(v => v.Zone)
                .Where(v => v.ZoneId == zoneId && 
                           v.NeedsBatteryReplacement == true && 
                           v.IsAvailable == true)
                .OrderBy(v => v.BatteryLevel)
                .ToListAsync();

            return vehicles.Select(MapToVehicleDto).ToList();
        }

        private VehicleDto MapToVehicleDto(Domain.Entities.Vehicle vehicle)
        {
            return new VehicleDto
            {
                Id = vehicle.Id,
                ExternalId = vehicle.ExternalId,
                RegistrationNumber = vehicle.RegistrationNumber,
                VehicleType = vehicle.VehicleType,
                ZoneId = vehicle.ZoneId,
                ZoneName = vehicle.Zone?.Name,
                CurrentParkingZoneId = vehicle.CurrentParkingZoneId,
                CurrentParkingZoneName = vehicle.CurrentParkingZone?.Name,
                Latitude = vehicle.Latitude,
                Longitude = vehicle.Longitude,
                BatteryLevel = vehicle.BatteryLevel,
                NeedsBatteryReplacement = vehicle.NeedsBatteryReplacement,
                IsAvailable = vehicle.IsAvailable,
                LastUpdated = vehicle.LastUpdated
            };
        }
    }
}
