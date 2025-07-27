using Microsoft.AspNetCore.Mvc;
using HoppyRoute.Application.DTOs;
using HoppyRoute.Application.Interfaces;

namespace HoppyRouteApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RegionsController : ControllerBase
    {
        private readonly IRegionService _regionService;

        public RegionsController(IRegionService regionService)
        {
            _regionService = regionService;
        }

        /// <summary>
        /// Haal alle landen met regio's op
        /// </summary>
        [HttpGet("countries")]
        public async Task<ActionResult<List<CountryDto>>> GetCountries()
        {
            var countries = await _regionService.GetCountriesAsync();
            return Ok(countries);
        }

        /// <summary>
        /// Haal regio's per land op
        /// </summary>
        [HttpGet("country/{countryCode}")]
        public async Task<ActionResult<List<RegionDto>>> GetRegionsByCountry(string countryCode)
        {
            var regions = await _regionService.GetRegionsByCountryAsync(countryCode);
            return Ok(regions);
        }

        /// <summary>
        /// Haal alle regio's op
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<RegionDto>>> GetRegions()
        {
            var regions = await _regionService.GetAllRegionsAsync();
            return Ok(regions);
        }

        /// <summary>
        /// Haal een specifieke regio op
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<RegionDto>> GetRegion(int id)
        {
            var region = await _regionService.GetRegionByIdAsync(id);
            
            if (region == null)
                return NotFound($"Regio met ID {id} niet gevonden");

            return Ok(region);
        }

        /// <summary>
        /// Haal zones per regio op
        /// </summary>
        [HttpGet("{id}/zones")]
        public async Task<ActionResult<List<ZoneDto>>> GetZonesByRegion(int id)
        {
            var zones = await _regionService.GetZonesByRegionAsync(id);
            return Ok(zones);
        }
    }
}
