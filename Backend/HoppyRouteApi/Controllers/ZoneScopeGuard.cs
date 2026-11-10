using System.Security.Claims;
using HoppyRoute.Application.Interfaces;

namespace HoppyRouteApi.Controllers
{
    /// <summary>
    /// Zone scoping for FleetManagers. Admins are global; a FleetManager is limited to the zone or region
    /// they are assigned to (read from the database, see IZoneScopeService); anyone else has no scope.
    /// One instance per request, so the scope is looked up at most once.
    /// </summary>
    public class ZoneScopeGuard
    {
        private readonly IZoneScopeService _scope;
        private IReadOnlyCollection<int>? _cached;

        public ZoneScopeGuard(IZoneScopeService scope)
        {
            _scope = scope;
        }

        /// <summary>The zone ids the caller may manage; null means unrestricted (Admin).</summary>
        public async Task<IReadOnlyCollection<int>?> AllowedZoneIdsAsync(ClaimsPrincipal user)
        {
            if (user.IsInRole(Roles.Admin))
                return null;

            if (!user.IsInRole(Roles.FleetManager))
                return Array.Empty<int>();

            var userId = user.GetUserId();
            if (userId == null)
                return Array.Empty<int>();

            return _cached ??= await _scope.GetAllowedZoneIdsAsync(userId.Value);
        }

        public async Task<bool> CanManageZoneAsync(ClaimsPrincipal user, int zoneId)
        {
            var allowed = await AllowedZoneIdsAsync(user);
            return allowed == null || allowed.Contains(zoneId);
        }
    }
}
