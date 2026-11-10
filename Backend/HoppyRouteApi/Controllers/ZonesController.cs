using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HoppyRoute.Application.DTOs;
using HoppyRoute.Application.Interfaces;

namespace HoppyRouteApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // read-only controller: any signed-in role (the swapper screens read vehicles and zones too)
    public class ZonesController : ControllerBase
    {
        private readonly IZoneService _zoneService;
        private readonly ILogger<ZonesController> _logger;

        public ZonesController(IZoneService zoneService, ILogger<ZonesController> logger)
        {
            _zoneService = zoneService;
            _logger = logger;
        }

        /// <summary>
        /// Haal beschikbare zones op
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<ZoneDto>>> GetZones()
        {
            var zones = await _zoneService.GetAllZonesAsync();
            return Ok(zones);
        }

        /// <summary>
        /// Haal een specifieke zone op
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ZoneDto>> GetZone(int id)
        {
            var zone = await _zoneService.GetZoneByIdAsync(id);
            if (zone == null)
                return NotFound($"Zone met ID {id} niet gevonden");

            return Ok(zone);
        }

        /// <summary>
        /// Haal het aantal zones op
        /// </summary>
        [HttpGet("count")]
        public async Task<ActionResult<object>> GetZonesCount()
        {
            try
            {
                var zones = await _zoneService.GetAllZonesAsync();
                return Ok(new { count = zones.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Onverwachte fout bij het ophalen van het aantal zones");
                return StatusCode(500, new { message = "Er is een fout opgetreden bij het ophalen van zones aantal" });
            }
        }
    }
}
