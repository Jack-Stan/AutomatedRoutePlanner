using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HoppyRoute.Application.DTOs;
using HoppyRoute.Application.Interfaces;
using HoppyRoute.Domain.Enums;

namespace HoppyRouteApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RouteStopsController : ControllerBase
    {
        private readonly IRouteStopService _routeStopService;

        public RouteStopsController(IRouteStopService routeStopService)
        {
            _routeStopService = routeStopService;
        }

        /// <summary>
        /// Markeer stop voltooid door swapper.
        /// Admin/FleetManager: any stop. BatterySwapper: only stops on a route assigned to them.
        /// </summary>
        [HttpPost("{id}/complete")]
        [Authorize(Roles = Roles.AnyRole)]
        public async Task<ActionResult> CompleteRouteStop(int id)
        {
            var denied = await DenyUnlessManagerOrAssignedSwapperAsync(id);
            if (denied != null)
                return denied;

            var success = await _routeStopService.CompleteRouteStopAsync(id);
            
            if (!success)
                return NotFound($"RouteStop met ID {id} niet gevonden of al voltooid");

            return Ok(new { message = "RouteStop succesvol voltooid" });
        }

        /// <summary>
        /// Zet een stop op onderweg (InProgress) of overgeslagen (Skipped).
        /// Afronden gaat via POST {id}/complete, zodat de tijdstempels kloppen.
        /// Admin/FleetManager: elke stop. BatterySwapper: alleen stops op een eigen route.
        /// </summary>
        [HttpPut("{id}/status")]
        [Authorize(Roles = Roles.AnyRole)]
        public async Task<ActionResult<RouteStopDto>> UpdateStatus(int id, [FromBody] UpdateRouteStopStatusRequest request)
        {
            var denied = await DenyUnlessManagerOrAssignedSwapperAsync(id);
            if (denied != null)
                return denied;

            if (request.Status is not (RouteStopStatus.InProgress or RouteStopStatus.Skipped))
                return BadRequest(new { message = "Alleen InProgress (2) en Skipped (4) zijn toegestaan; gebruik POST {id}/complete om af te ronden" });

            var current = await _routeStopService.GetRouteStopByIdAsync(id);
            if (current == null)
                return NotFound($"RouteStop met ID {id} niet gevonden");

            if (current.Status is RouteStopStatus.Completed or RouteStopStatus.Skipped)
                return Conflict(new { message = "Deze stop is al voltooid of overgeslagen" });

            var updated = await _routeStopService.UpdateRouteStopStatusAsync(id, request.Status);
            return updated == null ? NotFound($"RouteStop met ID {id} niet gevonden") : Ok(updated);
        }

        /// <summary>
        /// Zet of wis de instructie/opmerking van een stop (max 500 tekens). Alleen Admin/FleetManager;
        /// swappers lezen ze via het veld "notes" van de route.
        /// </summary>
        [HttpPut("{id}/instructions")]
        [Authorize(Roles = Roles.Managers)]
        public async Task<ActionResult<RouteStopDto>> UpdateInstructions(int id, [FromBody] UpdateRouteStopInstructionsRequest request)
        {
            if (!await _routeStopService.AddNotesToRouteStopAsync(id, request.Notes ?? string.Empty))
                return NotFound($"RouteStop met ID {id} niet gevonden");

            var updated = await _routeStopService.GetRouteStopByIdAsync(id);
            return updated == null ? NotFound($"RouteStop met ID {id} niet gevonden") : Ok(updated);
        }

        /// <summary>
        /// Managers may act on any stop. A swapper only on a stop of a route assigned to them:
        /// 404 when the stop does not exist, 403 when the route belongs to someone else, null when allowed.
        /// </summary>
        private async Task<ActionResult?> DenyUnlessManagerOrAssignedSwapperAsync(int stopId)
        {
            if (User.IsManager())
                return null;

            var ownerId = await _routeStopService.GetAssignedSwapperIdForStopAsync(stopId);
            if (ownerId == null)
                return NotFound($"RouteStop met ID {stopId} niet gevonden");

            return User.GetUserId() == ownerId ? null : Forbid();
        }
    }
}
