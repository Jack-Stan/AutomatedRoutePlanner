using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HoppyRoute.Application.DTOs;
using HoppyRoute.Application.Interfaces;
using System.Security.Claims;

namespace HoppyRouteApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RoutesController : ControllerBase
    {
        private readonly IRouteService _routeService;

        public RoutesController(IRouteService routeService)
        {
            _routeService = routeService;
        }

        /// <summary>
        /// Genereer routesuggestie
        /// </summary>
        [HttpPost("suggest")]
        [Authorize(Roles = Roles.Managers)]
        public async Task<ActionResult<RouteGenerationResponse>> SuggestRoute([FromBody] RouteGenerationRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Krijg de huidige gebruiker ID uit verschillende claim types
            var userIdClaim = User.FindFirst("sub") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized("Gebruiker niet geauthenticeerd");

            if (!int.TryParse(userIdClaim.Value, out int userId))
                return BadRequest("Ongeldige gebruiker ID");

            // Pass userId as separate parameter to GenerateRouteAsync
            var response = await _routeService.GenerateRouteAsync(request, userId);
            
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        /// <summary>
        /// Haal routesuggesties op per zone (voor BatterySwappers - alleen hun eigen routes)
        /// </summary>
        [HttpGet("suggestions")]
        public async Task<ActionResult<List<RouteDto>>> GetRouteSuggestions([FromQuery] int zoneId)
        {
            // Krijg de huidige gebruiker ID en rol uit claims
            var userIdClaim = User.FindFirst("sub") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized("Gebruiker niet geauthenticeerd");

            if (!int.TryParse(userIdClaim.Value, out int userId))
                return BadRequest("Ongeldige gebruiker ID");

            // Check role claim (supports both string and numeric formats for backwards compatibility)
            var roleClaim = User.FindFirst(ClaimTypes.Role);
            if (roleClaim == null)
                return Unauthorized("Geen rol gevonden in token");

            string roleValue = roleClaim.Value;

            // For BatterySwappers, only return routes assigned to them
            if (roleValue == "BatterySwapper" || roleValue == "2")
            {
                var routes = await _routeService.GetRoutesForSwapperAsync(userId, zoneId);
                return Ok(routes);
            }
            
            // For other roles, return all routes in zone
            var allRoutes = await _routeService.GetRouteSuggestionsAsync(zoneId);
            return Ok(allRoutes);
        }

        /// <summary>
        /// Haal alle routes op (voor Admin en FleetManager)
        /// </summary>
        [HttpGet("all")]
        [Authorize(Roles = Roles.Managers)]
        public async Task<ActionResult<List<RouteDto>>> GetAllRoutes()
        {
            var routes = await _routeService.GetAllRoutesAsync();
            return Ok(routes);
        }

        /// <summary>
        /// Fleetmanager bevestigt route
        /// </summary>
        [HttpPost("{id}/confirm")]
        [Authorize(Roles = Roles.AnyRole)]
        public async Task<ActionResult<RouteDto>> ConfirmRoute(int id)
        {
            // RoutesScreen shows "Route Bevestigen" to every role; a swapper may only confirm their own route.
            var denied = await DenyUnlessManagerOrAssignedSwapperAsync(id);
            if (denied != null)
                return denied;

            var route = await _routeService.ConfirmRouteAsync(id);
            
            if (route == null)
                return NotFound($"Route met ID {id} niet gevonden");

            return Ok(route);
        }

        /// <summary>
        /// Swapper haalt bevestigde route op
        /// </summary>
        [HttpGet("today")]
        public async Task<ActionResult<RouteDto>> GetTodaysRoute([FromQuery] int swapperId)
        {
            // A swapper may only look up their own route of the day.
            if (!User.IsManager() && User.GetUserId() != swapperId)
                return Forbid();

            var route = await _routeService.GetTodaysRouteForSwapperAsync(swapperId);
            
            if (route == null)
                return NotFound($"Geen route gevonden voor swapper {swapperId} vandaag");

            return Ok(route);
        }

        /// <summary>
        /// Haal een specifieke route op
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<RouteDto>> GetRoute(int id)
        {
            var denied = await DenyUnlessManagerOrAssignedSwapperAsync(id);
            if (denied != null)
                return denied;

            var route = await _routeService.GetRouteByIdAsync(id);
            
            if (route == null)
                return NotFound($"Route met ID {id} niet gevonden");

            return Ok(route);
        }

        /// <summary>
        /// FleetManager/Admin keurt route goed
        /// </summary>
        [HttpPost("{id}/approve")]
        [Authorize(Roles = Roles.Managers)]
        public async Task<ActionResult<RouteDto>> ApproveRoute(int id, [FromBody] RouteApprovalRequest? request = null)
        {
            // The approver is the caller, never a value from the body.
            var approverId = User.GetUserId();
            if (approverId == null)
                return Unauthorized("Gebruiker niet geauthenticeerd");

            var route = await _routeService.ApproveRouteAsync(id, approverId.Value, request?.Notes);
            if (route == null)
                return NotFound($"Route met ID {id} niet gevonden");

            return Ok(route);
        }

        /// <summary>
        /// FleetManager/Admin wijst route af
        /// </summary>
        [HttpPost("{id}/reject")]
        [Authorize(Roles = Roles.Managers)]
        public async Task<ActionResult<RouteDto>> RejectRoute(int id, [FromBody] RouteApprovalRequest? request = null)
        {
            // The approver is the caller, never a value from the body.
            var approverId = User.GetUserId();
            if (approverId == null)
                return Unauthorized("Gebruiker niet geauthenticeerd");

            var route = await _routeService.RejectRouteAsync(id, approverId.Value, request?.Notes);
            if (route == null)
                return NotFound($"Route met ID {id} niet gevonden");

            return Ok(route);
        }

        /// <summary>
        /// Routes die goedkeuring nodig hebben
        /// </summary>
        [HttpGet("pending-approval")]
        [Authorize(Roles = Roles.Managers)]
        public async Task<ActionResult<List<RouteDto>>> GetRoutesPendingApproval([FromQuery] int? zoneId = null)
        {
            var routes = await _routeService.GetRoutesPendingApprovalAsync(zoneId);
            return Ok(routes);
        }

        /// <summary>
        /// Managers may act on any route. Anyone else only on a route assigned to them:
        /// 404 when the route does not exist, 403 when it belongs to someone else, null when allowed.
        /// </summary>
        private async Task<ActionResult?> DenyUnlessManagerOrAssignedSwapperAsync(int routeId)
        {
            if (User.IsManager())
                return null;

            var ownerId = await _routeService.GetAssignedSwapperIdAsync(routeId);
            if (ownerId == null)
                return NotFound($"Route met ID {routeId} niet gevonden");

            return User.GetUserId() == ownerId ? null : Forbid();
        }
    }
}
