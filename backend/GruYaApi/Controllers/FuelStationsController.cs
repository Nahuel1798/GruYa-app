using Microsoft.AspNetCore.Mvc;
using GruYaApi.Service;

namespace GruYaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FuelStationsController : ControllerBase
    {
        private readonly OverpassService _overpassService;

        public FuelStationsController(OverpassService overpassService)
        {
            _overpassService = overpassService;
        }

        // GET api/fuelstations?lat=...&lon=...&radius=...
        [HttpGet]
        public async Task<IActionResult> GetFuelStations(
            [FromQuery] double lat,
            [FromQuery] double lon,
            [FromQuery] int radius = 5000)
        {
            if (lat < -90 || lat > 90)
                return BadRequest("Latitud inválida");

            if (lon < -180 || lon > 180)
                return BadRequest("Longitud inválida");

            if (radius <= 0 || radius > 50000)
                return BadRequest("Radio inválido (1 - 50000)");

            try
            {
                var stations = await _overpassService
                    .GetFuelStationsAsync(lat, lon, radius);

                return Ok(stations);
            }
            catch (Exception ex)
            {
                return StatusCode(503, new
                {
                    message = "Error consultando Overpass API",
                    detail = ex.Message
                });
            }
        }
    }
}