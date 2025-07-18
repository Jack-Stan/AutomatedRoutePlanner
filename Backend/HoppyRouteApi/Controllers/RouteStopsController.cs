using Microsoft.AspNetCore.Mvc;
using HoppyRoute.Application.Interfaces;

namespace HoppyRouteApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RouteStopsController : ControllerBase
    {
        private readonly IRouteStopService _routeStopService;

        public RouteStopsController(IRouteStopService routeStopService)
        {
            _routeStopService = routeStopService;
        }

        /// <summary>
        /// Markeer stop voltooid door swapper
        /// </summary>
        [HttpPost("{id}/complete")]
        public async Task<ActionResult> CompleteRouteStop(int id)
        {
            var success = await _routeStopService.CompleteRouteStopAsync(id);
            
            if (!success)
                return NotFound($"RouteStop met ID {id} niet gevonden of al voltooid");

            return Ok(new { message = "RouteStop succesvol voltooid" });
        }
    }
}
