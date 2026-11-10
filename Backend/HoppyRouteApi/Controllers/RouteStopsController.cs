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
        private readonly ZoneScopeGuard _zoneScope;

        public RouteStopsController(IRouteStopService routeStopService, ZoneScopeGuard zoneScope)
        {
            _routeStopService = routeStopService;
            _zoneScope = zoneScope;
        }

        /// <summary>
        /// Markeer stop voltooid door swapper.
        /// Admin: any stop. FleetManager: stops in their own zone/region. BatterySwapper: stops on their own route.
        /// </summary>
        [HttpPost("{id}/complete")]
        [Authorize(Roles = Roles.AnyRole)]
        public async Task<ActionResult> CompleteRouteStop(int id)
        {
            var denied = await AuthorizeStopAsync(id, allowAssignedSwapper: true);
            if (denied != null)
                return denied;

            var success = await _routeStopService.CompleteRouteStopAsync(id);
            
            if (!success)
                return NotFound($"RouteStop met ID {id} niet gevonden of al voltooid");

            return Ok(new { message = "RouteStop succesvol voltooid" });
        }

        /// <summary>
        /// Zet een stop op onderweg (InProgress) of overgeslagen (Skipped), bij Skipped met een optionele
        /// reden (max 500 tekens, opgeslagen als SkipReason; bij InProgress is een reden een 400).
        /// Afronden gaat via POST {id}/complete, zodat de tijdstempels kloppen.
        /// Admin: elke stop. FleetManager: stops in de eigen zone/regio. BatterySwapper: alleen stops op een eigen route.
        /// </summary>
        [HttpPut("{id}/status")]
        [Authorize(Roles = Roles.AnyRole)]
        public async Task<ActionResult<RouteStopDto>> UpdateStatus(int id, [FromBody] UpdateRouteStopStatusRequest request)
        {
            var denied = await AuthorizeStopAsync(id, allowAssignedSwapper: true);
            if (denied != null)
                return denied;

            if (request.Status is not (RouteStopStatus.InProgress or RouteStopStatus.Skipped))
                return BadRequest(new { message = "Alleen InProgress (2) en Skipped (4) zijn toegestaan; gebruik POST {id}/complete om af te ronden" });

            if (request.Status != RouteStopStatus.Skipped && !string.IsNullOrWhiteSpace(request.Reason))
                return BadRequest(new { message = "Een reden kan alleen samen met status Skipped (4) worden opgegeven" });

            var current = await _routeStopService.GetRouteStopByIdAsync(id);
            if (current == null)
                return NotFound($"RouteStop met ID {id} niet gevonden");

            if (current.Status is RouteStopStatus.Completed or RouteStopStatus.Skipped)
                return Conflict(new { message = "Deze stop is al voltooid of overgeslagen" });

            var updated = await _routeStopService.UpdateRouteStopStatusAsync(id, request.Status, request.Reason);
            return updated == null ? NotFound($"RouteStop met ID {id} niet gevonden") : Ok(updated);
        }

        /// <summary>
        /// Zet of wis de instructie/opmerking van een stop (max 500 tekens). Admin, of FleetManager voor stops in de
        /// eigen zone/regio; swappers lezen ze via het veld "notes" van de route.
        /// </summary>
        [HttpPut("{id}/instructions")]
        [Authorize(Roles = Roles.Managers)]
        public async Task<ActionResult<RouteStopDto>> UpdateInstructions(int id, [FromBody] UpdateRouteStopInstructionsRequest request)
        {
            var denied = await AuthorizeStopAsync(id, allowAssignedSwapper: false);
            if (denied != null)
                return denied;

            if (!await _routeStopService.AddNotesToRouteStopAsync(id, request.Notes ?? string.Empty))
                return NotFound($"RouteStop met ID {id} niet gevonden");

            var updated = await _routeStopService.GetRouteStopByIdAsync(id);
            return updated == null ? NotFound($"RouteStop met ID {id} niet gevonden") : Ok(updated);
        }

        /// <summary>
        /// Who may act on a stop: Admin any stop; a FleetManager stops whose route is in their zone/region;
        /// a BatterySwapper (when allowed) stops on a route assigned to them.
        /// 404 when the stop does not exist, 403 when not allowed, null when allowed.
        /// </summary>
        private async Task<ActionResult?> AuthorizeStopAsync(int stopId, bool allowAssignedSwapper)
        {
            var access = await _routeStopService.GetAccessInfoForStopAsync(stopId);
            if (access == null)
                return NotFound($"RouteStop met ID {stopId} niet gevonden");

            if (User.IsInRole(Roles.Admin))
                return null;

            if (User.IsInRole(Roles.FleetManager))
                return await _zoneScope.CanManageZoneAsync(User, access.ZoneId) ? null : Forbid();

            return allowAssignedSwapper && User.GetUserId() == access.AssignedSwapperId ? null : Forbid();
        }
    }
}
