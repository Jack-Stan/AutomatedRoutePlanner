using Microsoft.AspNetCore.Mvc;
using HoppyRoute.Application.DTOs;
using HoppyRoute.Application.Interfaces;

namespace HoppyRouteApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehiclesController : ControllerBase
    {
        private readonly IVehicleService _vehicleService;

        public VehiclesController(IVehicleService vehicleService)
        {
            _vehicleService = vehicleService;
        }

        /// <summary>
        /// Voertuigen met batterij < drempel in zone
        /// </summary>
        [HttpGet("lowbattery")]
        public async Task<ActionResult<List<VehicleDto>>> GetLowBatteryVehicles([FromQuery] int zoneId, [FromQuery] int batteryThreshold = 25)
        {
            var vehicles = await _vehicleService.GetLowBatteryVehiclesAsync(zoneId, batteryThreshold);
            return Ok(vehicles);
        }

        /// <summary>
        /// Alle voertuigen in een zone
        /// </summary>
        [HttpGet("zone/{zoneId}")]
        public async Task<ActionResult<List<VehicleDto>>> GetVehiclesByZone(int zoneId)
        {
            var vehicles = await _vehicleService.GetVehiclesByZoneAsync(zoneId);
            return Ok(vehicles);
        }

        /// <summary>
        /// Haal een specifiek voertuig op
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<VehicleDto>> GetVehicle(int id)
        {
            var vehicle = await _vehicleService.GetVehicleByIdAsync(id);
            if (vehicle == null)
                return NotFound($"Voertuig met ID {id} niet gevonden");

            return Ok(vehicle);
        }
    }
}
