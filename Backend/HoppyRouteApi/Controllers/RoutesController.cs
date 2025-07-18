using Microsoft.AspNetCore.Mvc;
using HoppyRoute.Application.DTOs;
using HoppyRoute.Application.Interfaces;

namespace HoppyRouteApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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

            var response = await _routeService.GenerateRouteAsync(request);
            
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        /// <summary>
        /// Haal routesuggesties op per zone
        /// </summary>
        [HttpGet("suggestions")]
        public async Task<ActionResult<List<RouteDto>>> GetRouteSuggestions([FromQuery] int zoneId)
        {
            var routes = await _routeService.GetRouteSuggestionsAsync(zoneId);
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
    }
}
