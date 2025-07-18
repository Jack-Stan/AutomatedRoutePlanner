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
            var zone = await _context.Zones.FindAsync(zoneId);

            if (zone == null)
                return null;

            return new ZoneDto
            {
                Id = zone.Id,
                Name = zone.Name,
                CountryCode = zone.CountryCode,
                GeoJsonBoundary = zone.GeoJsonBoundary
            };
        }
    }
}
