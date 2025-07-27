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
        /// ALLE voertuigen met batterij < drempel (alle zones)
        /// </summary>
        [HttpGet("lowbattery/all")]
        public async Task<ActionResult<List<VehicleDto>>> GetAllLowBatteryVehicles([FromQuery] int batteryThreshold = 25)
        {
            var vehicles = await _vehicleService.GetAllLowBatteryVehiclesAsync(batteryThreshold);
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
        /// ALLE voertuigen (alle zones)
        /// </summary>
        [HttpGet("all")]
        public async Task<ActionResult<List<VehicleDto>>> GetAllVehicles()
        {
            var vehicles = await _vehicleService.GetAllVehiclesAsync();
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

        /// <summary>
        /// Beschikbare voertuigen in een zone
        /// </summary>
        [HttpGet("available/zone/{zoneId}")]
        public async Task<ActionResult<List<VehicleDto>>> GetAvailableVehicles(int zoneId)
        {
            var vehicles = await _vehicleService.GetAvailableVehiclesAsync(zoneId);
            return Ok(vehicles);
        }

        /// <summary>
        /// Voertuigen die batterij vervanging nodig hebben
        /// </summary>
        [HttpGet("needing-battery-replacement/zone/{zoneId}")]
        public async Task<ActionResult<List<VehicleDto>>> GetVehiclesNeedingBatteryReplacement(int zoneId)
        {
            var vehicles = await _vehicleService.GetVehiclesNeedingBatteryReplacementAsync(zoneId);
            return Ok(vehicles);
        }

        /// <summary>
        /// Debug endpoint: voertuig statistieken per zone
        /// </summary>
        [HttpGet("stats/zone/{zoneId}")]
        public async Task<ActionResult<object>> GetVehicleStats(int zoneId)
        {
            var allVehicles = await _vehicleService.GetVehiclesByZoneAsync(zoneId);
            var lowBatteryVehicles = await _vehicleService.GetLowBatteryVehiclesAsync(zoneId);
            var availableVehicles = await _vehicleService.GetAvailableVehiclesAsync(zoneId);
            var needingBatteryReplacement = await _vehicleService.GetVehiclesNeedingBatteryReplacementAsync(zoneId);

            return Ok(new
            {
                ZoneId = zoneId,
                TotalVehicles = allVehicles.Count,
                LowBatteryVehicles = lowBatteryVehicles.Count,
                AvailableVehicles = availableVehicles.Count,
                NeedingBatteryReplacement = needingBatteryReplacement.Count,
                BatteryDistribution = allVehicles
                    .GroupBy(v => v.BatteryLevel <= 25 ? "Low (≤25%)" : v.BatteryLevel <= 50 ? "Medium (26-50%)" : "High (>50%)")
                    .ToDictionary(g => g.Key, g => g.Count())
            });
        }
    }
}
