using Microsoft.EntityFrameworkCore;
using HoppyRoute.Application.DTOs;
using HoppyRoute.Application.Interfaces;
using HoppyRoute.Infrastructure.Data;

namespace HoppyRoute.Application.Services
{
    public class ZoneService : IZoneService
    {
        private readonly HoppyDbContext _context;

        public ZoneService(HoppyDbContext context)
        {
            _context = context;
        }

        public async Task<List<ZoneDto>> GetAllZonesAsync()
        {
            var zones = await _context.Zones
                .OrderBy(z => z.Name)
                .ToListAsync();

            return zones.Select(z => new ZoneDto
            {
                Id = z.Id,
                Name = z.Name,
                CountryCode = z.CountryCode,
                GeoJsonBoundary = z.GeoJsonBoundary
            }).ToList();
        }

        public async Task<ZoneDto?> GetZoneByIdAsync(int zoneId)
        {
            var zone = await _context.Zones
                .Include(z => z.Region)
                .FirstOrDefaultAsync(z => z.Id == zoneId);

            if (zone == null)
                return null;

            return new ZoneDto
            {
                Id = zone.Id,
                Name = zone.Name,
                CountryCode = zone.CountryCode,
                GeoJsonBoundary = zone.GeoJsonBoundary,
                RegionId = zone.RegionId,
                RegionName = zone.Region?.Name ?? ""
            };
        }

        public async Task<List<ZoneDto>> GetZonesByRegionAsync(int regionId)
        {
            var zones = await _context.Zones
                .Include(z => z.Region)
                .Where(z => z.RegionId == regionId)
                .OrderBy(z => z.Name)
                .ToListAsync();

            return zones.Select(z => new ZoneDto
            {
                Id = z.Id,
                Name = z.Name,
                CountryCode = z.CountryCode,
                GeoJsonBoundary = z.GeoJsonBoundary,
                RegionId = z.RegionId,
                RegionName = z.Region?.Name ?? ""
            }).ToList();
        }

        public async Task<List<ParkingZoneDto>> GetParkingZonesByZoneAsync(int zoneId)
        {
            var parkingZones = await _context.ParkingZones
                .Include(pz => pz.Zone)
                .Where(pz => pz.ZoneId == zoneId && pz.IsActive)
                .OrderBy(pz => pz.Name)
                .ToListAsync();

            return parkingZones.Select(pz => new ParkingZoneDto
            {
                Id = pz.Id,
                Name = pz.Name,
                Description = pz.Description,
                Latitude = pz.Latitude,
                Longitude = pz.Longitude,
                RadiusMeters = pz.RadiusMeters,
                MaxCapacity = pz.MaxCapacity,
                CurrentVehicleCount = pz.CurrentVehicleCount,
                IsActive = pz.IsActive,
                CreatedAt = pz.CreatedAt,
                ZoneId = pz.ZoneId,
                ZoneName = pz.Zone?.Name ?? ""
            }).ToList();
        }
    }
}
