using Microsoft.EntityFrameworkCore;
using HoppyRoute.Application.Interfaces;
using HoppyRoute.Domain.Enums;
using HoppyRoute.Infrastructure.Data;

namespace HoppyRoute.Application.Services
{
    public class ZoneScopeService : IZoneScopeService
    {
        private readonly HoppyDbContext _context;

        public ZoneScopeService(HoppyDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyCollection<int>> GetAllowedZoneIdsAsync(int fleetManagerId)
        {
            // Read from the database, not the token: an admin changing an assignment (or deactivating
            // the account) must take effect immediately, and the token carries no region.
            var manager = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == fleetManagerId && u.IsActive && u.Role == UserRole.FleetManager)
                .Select(u => new { u.AssignedZoneId, u.AssignedRegionId })
                .FirstOrDefaultAsync();

            if (manager == null)
                return Array.Empty<int>();

            // The narrowest assignment wins: a zone is that zone only, a region is every zone in it.
            if (manager.AssignedZoneId.HasValue)
                return new[] { manager.AssignedZoneId.Value };

            if (manager.AssignedRegionId.HasValue)
            {
                var regionId = manager.AssignedRegionId.Value;
                return await _context.Zones
                    .AsNoTracking()
                    .Where(z => z.RegionId == regionId)
                    .Select(z => z.Id)
                    .ToListAsync();
            }

            // No assignment: no scope. Everything is out of scope (fail closed).
            return Array.Empty<int>();
        }
    }
}
