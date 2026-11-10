using System.Security.Claims;
using HoppyRoute.Domain.Enums;

namespace HoppyRouteApi.Controllers
{
    /// <summary>
    /// Role names for [Authorize(Roles = ...)]. They match the "role" claim AuthService writes
    /// into the JWT (UserRole.ToString()).
    /// </summary>
    public static class Roles
    {
        public const string Admin = nameof(UserRole.Admin);
        public const string FleetManager = nameof(UserRole.FleetManager);
        public const string BatterySwapper = nameof(UserRole.BatterySwapper);

        /// <summary>Admin and FleetManager: manage zones, regions and routes.</summary>
        public const string Managers = Admin + "," + FleetManager;

        /// <summary>Every real role (a token with some other role value gets 403).</summary>
        public const string AnyRole = Admin + "," + FleetManager + "," + BatterySwapper;
    }

    public static class ClaimsPrincipalRoleExtensions
    {
        public static bool IsManager(this ClaimsPrincipal user) =>
            user.IsInRole(Roles.Admin) || user.IsInRole(Roles.FleetManager);

        /// <summary>The caller's user id from the "sub" claim, or null when the token has none.</summary>
        public static int? GetUserId(this ClaimsPrincipal user)
        {
            var value = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(value, out var id) ? id : null;
        }
    }
}
