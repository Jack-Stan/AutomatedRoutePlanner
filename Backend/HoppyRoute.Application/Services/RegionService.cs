using Microsoft.EntityFrameworkCore;
using HoppyRoute.Application.DTOs;
using HoppyRoute.Application.Interfaces;
using HoppyRoute.Infrastructure.Data;
using HoppyRoute.Domain.Entities;

namespace HoppyRoute.Application.Services
{
    public class RegionService : IRegionService
    {
        private readonly HoppyDbContext _context;

        public RegionService(HoppyDbContext context)
        {
            _context = context;
        }

        public async Task<List<RegionDto>> GetAllRegionsAsync()
        {
            var regions = await _context.Regions
                .Include(r => r.Zones)
                .Where(r => r.IsActive)
                .OrderBy(r => r.Country)
                .ThenBy(r => r.Name)
                .ToListAsync();

            return regions.Select(MapToRegionDto).ToList();
        }

        public async Task<RegionDto?> GetRegionByIdAsync(int regionId)
        {
            var region = await _context.Regions
                .Include(r => r.Zones)
                .FirstOrDefaultAsync(r => r.Id == regionId && r.IsActive);

            return region != null ? MapToRegionDto(region) : null;
        }

        public async Task<List<CountryDto>> GetCountriesAsync()
        {
            var regions = await _context.Regions
                .Include(r => r.Zones)
                .Where(r => r.IsActive)
                .OrderBy(r => r.Country)
                .ThenBy(r => r.Name)
                .ToListAsync();

            var countries = regions
                .GroupBy(r => new { r.CountryCode, r.Country })
                .Select(g => new CountryDto
                {
                    CountryCode = g.Key.CountryCode,
                    CountryName = g.Key.Country,
                    Regions = g.Select(MapToRegionDto).ToList()
                })
                .OrderBy(c => c.CountryName)
                .ToList();

            return countries;
        }

        public async Task<List<RegionDto>> GetRegionsByCountryAsync(string countryCode)
        {
            var regions = await _context.Regions
                .Include(r => r.Zones)
                .Where(r => r.CountryCode == countryCode && r.IsActive)
                .OrderBy(r => r.Name)
                .ToListAsync();

            return regions.Select(MapToRegionDto).ToList();
        }

        public async Task<RegionDto> CreateRegionAsync(CreateRegionDto request)
        {
            var region = new Region
            {
                Name = request.Name,
                Country = request.Country,
                CountryCode = request.CountryCode,
                Description = request.Description,
                IsActive = request.IsActive,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                CreatedAt = DateTime.UtcNow
            };

            _context.Regions.Add(region);
            await _context.SaveChangesAsync();

            return MapToRegionDto(region);
        }

        public async Task<RegionDto?> UpdateRegionAsync(int regionId, UpdateRegionDto request)
        {
            var region = await _context.Regions.FirstOrDefaultAsync(r => r.Id == regionId);
            if (region == null) return null;

            region.Name = request.Name;
            region.Country = request.Country;
            region.CountryCode = request.CountryCode;
            region.Description = request.Description;
            region.IsActive = request.IsActive;
            region.Latitude = request.Latitude;
            region.Longitude = request.Longitude;

            await _context.SaveChangesAsync();
            return MapToRegionDto(region);
        }

        public async Task<bool> DeleteRegionAsync(int regionId)
        {
            var region = await _context.Regions.FirstOrDefaultAsync(r => r.Id == regionId);
            if (region == null) return false;

            region.IsActive = false; // Soft delete
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<ZoneDto>> GetZonesByRegionAsync(int regionId)
        {
            var zones = await _context.Zones
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

        public async Task<List<VehicleDto>> GetVehiclesByRegionAsync(GetVehiclesInRegionRequest request)
        {
            var query = _context.Vehicles
                .Include(v => v.Zone)
                .ThenInclude(z => z!.Region)
                .Where(v => v.Zone != null && v.Zone.RegionId == request.RegionId);

            if (request.BatteryThreshold.HasValue)
            {
                query = query.Where(v => v.BatteryLevel <= request.BatteryThreshold.Value);
            }

            if (request.IsAvailable.HasValue)
            {
                query = query.Where(v => v.IsAvailable == request.IsAvailable.Value);
            }

            var vehicles = await query.ToListAsync();

            return vehicles.Select(v => new VehicleDto
            {
                Id = v.Id,
                ExternalId = v.ExternalId,
                RegistrationNumber = v.RegistrationNumber,
                VehicleType = v.VehicleType,
                ZoneId = v.ZoneId,
                ZoneName = v.Zone?.Name ?? "",
                CurrentParkingZoneId = v.CurrentParkingZoneId,
                CurrentParkingZoneName = v.CurrentParkingZone?.Name,
                Latitude = v.Latitude,
                Longitude = v.Longitude,
                BatteryLevel = v.BatteryLevel,
                NeedsBatteryReplacement = v.NeedsBatteryReplacement,
                IsAvailable = v.IsAvailable,
                LastUpdated = v.LastUpdated
            }).ToList();
        }

        public async Task<List<UserDto>> GetUsersByRegionAsync(int regionId)
        {
            var users = await _context.Users
                .Include(u => u.AssignedZone)
                .ThenInclude(z => z!.Region)
                .Where(u => u.AssignedZone != null && u.AssignedZone.RegionId == regionId && u.IsActive)
                .ToListAsync();

            return users.Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                Role = u.Role,
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt,
                AssignedZoneId = u.AssignedZoneId,
                AssignedZoneName = u.AssignedZone?.Name
            }).ToList();
        }

        private RegionDto MapToRegionDto(Region region)
        {
            return new RegionDto
            {
                Id = region.Id,
                Name = region.Name,
                Country = region.Country,
                CountryCode = region.CountryCode,
                Description = region.Description,
                IsActive = region.IsActive,
                Latitude = region.Latitude,
                Longitude = region.Longitude,
                Zones = region.Zones?.Select(z => new ZoneDto
                {
                    Id = z.Id,
                    Name = z.Name,
                    CountryCode = z.CountryCode,
                    GeoJsonBoundary = z.GeoJsonBoundary,
                    RegionId = z.RegionId,
                    RegionName = region.Name
                }).ToList() ?? new List<ZoneDto>(),
                TotalZones = region.Zones?.Count ?? 0,
                TotalVehicles = region.Zones?.Sum(z => z.Vehicles?.Count ?? 0) ?? 0,
                TotalUsers = region.AssignedUsers?.Count ?? 0
            };
        }
    }
}
