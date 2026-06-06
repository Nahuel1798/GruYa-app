using GruYaApi.Data;
using GruYaApi.DTOs.Requests;
using GruYaApi.DTOs.Responses;
using GruYaApi.Models;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GruYaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class VehiclesController : ControllerBase
    {
        private readonly DataContext _context;

        public VehiclesController(DataContext context)
        {
            _context = context;
        }

        // GET: api/vehicles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VehicleResponse>>> GetVehicles()
        {
            var vehicles = await _context
                .Vehicles.AsNoTracking()
                .ProjectToType<VehicleResponse>()
                .ToListAsync();

            return Ok(vehicles);
        }

        // GET: api/vehicles/5
        [HttpGet("{id}")]
        public async Task<ActionResult<VehicleResponse>> GetVehicle(int id)
        {
            var vehicle = await _context
                .Vehicles.AsNoTracking()
                .Where(v => v.Id == id)
                .ProjectToType<VehicleResponse>()
                .FirstOrDefaultAsync();

            if (vehicle == null)
            {
                return NotFound(new { message = "Vehículo no encontrado" });
            }

            return Ok(vehicle);
        }

        // POST: api/vehicles/create
        [HttpPost]
        public async Task<ActionResult<VehicleResponse>> CreateVehicle(CreateVehicleRequest request)
        {
            var existPlate = await _context.Vehicles.AnyAsync(v =>
                v.LicensePlate == request.LicensePlate
            );
            if (existPlate)
            {
                return BadRequest(new { message = "La placa ya existe" });
            }

            var vehicle = request.Adapt<Vehicle>();
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            var response = vehicle.Adapt<VehicleResponse>();
            return CreatedAtAction(nameof(GetVehicle), new { id = vehicle.Id }, response);
        }

        // PUT: api/vehicles/5
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateVehicle(int id, CreateVehicleRequest request)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);

            if (vehicle == null)
            {
                return NotFound(new { message = "Vehículo no encontrado" });
            }

            var existsPlate = await _context.Vehicles.AnyAsync(v =>
                v.LicensePlate == request.LicensePlate && v.Id != id
            );

            if (existsPlate)
            {
                return BadRequest(new { message = "La patente ya existe" });
            }

            request.Adapt(vehicle);

            await _context.SaveChangesAsync();

            return Ok(vehicle.Adapt<VehicleResponse>());
        }

        // DELETE: api/vehicles/5
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteVehicle(int id)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);

            if (vehicle == null)
            {
                return NotFound(new { message = "Vehículo no encontrado" });
            }

            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Vehículo eliminado correctamente" });
        }
    }
}
