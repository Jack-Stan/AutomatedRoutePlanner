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
        public async Task<ActionResult<List<RouteDto>>> GetAllRoutes()
        {
            // Debug: Print all claims
            Console.WriteLine("=== ALL CLAIMS DEBUG ===");
            foreach (var claim in User.Claims)
            {
                Console.WriteLine($"Claim Type: '{claim.Type}' = Value: '{claim.Value}'");
            }
            Console.WriteLine("=== END CLAIMS DEBUG ===");
            
            // Check if user is admin or fleet manager - support both string and numeric role formats
            var userRoleClaim = User.FindFirst("role") ?? 
                                User.FindFirst(ClaimTypes.Role) ??
                                User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role");
            var roleValue = userRoleClaim?.Value;
            
            Console.WriteLine($"Role claim found: '{roleValue}' (from claim type: '{userRoleClaim?.Type}')");
            
            bool isAuthorized = false;
            if (roleValue != null)
            {
                // Check string format first (new format)
                if (roleValue == "Admin" || roleValue == "FleetManager")
                {
                    isAuthorized = true;
                    Console.WriteLine($"Authorized via string format: {roleValue}");
                }
                // Check numeric format for backwards compatibility (old format)
                else if (int.TryParse(roleValue, out int numericRole))
                {
                    isAuthorized = numericRole == 0 || numericRole == 1; // Admin=0, FleetManager=1
                    Console.WriteLine($"Checked numeric format: {numericRole}, authorized: {isAuthorized}");
                }
            }
            
            Console.WriteLine($"Final authorization result: {isAuthorized}");
            
            if (!isAuthorized)
            {
                return StatusCode(403, new { message = "Alleen administrators en fleet managers kunnen alle routes bekijken" });
            }

            var routes = await _routeService.GetAllRoutesAsync();
            return Ok(routes);
        }

        /// <summary>
        /// Fleetmanager bevestigt route
        /// </summary>
        [HttpPost("{id}/confirm")]
        public async Task<ActionResult<RouteDto>> ConfirmRoute(int id)
        {
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
            var route = await _routeService.GetRouteByIdAsync(id);
            
            if (route == null)
                return NotFound($"Route met ID {id} niet gevonden");

            return Ok(route);
        }

        /// <summary>
        /// FleetManager/Admin keurt route goed
        /// </summary>
        [HttpPost("{id}/approve")]
        public async Task<ActionResult<RouteDto>> ApproveRoute(int id, [FromBody] RouteApprovalRequest request)
        {
            var route = await _routeService.ApproveRouteAsync(id, request.ApprovedBy, request.Notes);
            if (route == null)
                return NotFound($"Route met ID {id} niet gevonden");

            return Ok(route);
        }

        /// <summary>
        /// FleetManager/Admin wijst route af
        /// </summary>
        [HttpPost("{id}/reject")]
        public async Task<ActionResult<RouteDto>> RejectRoute(int id, [FromBody] RouteApprovalRequest request)
        {
            var route = await _routeService.RejectRouteAsync(id, request.ApprovedBy, request.Notes);
            if (route == null)
                return NotFound($"Route met ID {id} niet gevonden");

            return Ok(route);
        }

        /// <summary>
        /// Routes die goedkeuring nodig hebben
        /// </summary>
        [HttpGet("pending-approval")]
        public async Task<ActionResult<List<RouteDto>>> GetRoutesPendingApproval([FromQuery] int? zoneId = null)
        {
            var routes = await _routeService.GetRoutesPendingApprovalAsync(zoneId);
            return Ok(routes);
        }
    }
}
