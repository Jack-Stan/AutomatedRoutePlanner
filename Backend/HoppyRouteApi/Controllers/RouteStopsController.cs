using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HoppyRoute.Application.Interfaces;

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
            if (!User.IsManager())
            {
                var ownerId = await _routeStopService.GetAssignedSwapperIdForStopAsync(id);
                if (ownerId == null)
                    return NotFound($"RouteStop met ID {id} niet gevonden of al voltooid");
                if (User.GetUserId() != ownerId)
                    return Forbid();
            }

            var success = await _routeStopService.CompleteRouteStopAsync(id);
            
            if (!success)
                return NotFound($"RouteStop met ID {id} niet gevonden of al voltooid");

            return Ok(new { message = "RouteStop succesvol voltooid" });
        }
    }
}
