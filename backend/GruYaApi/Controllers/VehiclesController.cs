using GruYaApi.Data;
using GruYaApi.DTOs.Requests;
using GruYaApi.DTOs.Responses;
using GruYaApi.Filters;
using GruYaApi.Models;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.IO;
using System;

namespace GruYaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [ServiceFilter(typeof(UserExists))]
    public class VehiclesController : ControllerBase
    {
        private readonly DataContext _context;
        private const string UserIdKey = "idUsuario";

        public VehiclesController(DataContext context)
        {
            _context = context;
        }

        // GET: api/vehicles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VehicleResponse>>> GetVehicles()
        {
            var userId = (int)HttpContext.Items[UserIdKey]!;

            var vehicles = await _context
                .Vehicles.AsNoTracking()
                .Where(v => v.UserId == userId)
                .ProjectToType<VehicleResponse>()
                .ToListAsync();

            return Ok(vehicles);
        }

        // GET: api/vehicles/5
        [HttpGet("{id}")]
        public async Task<ActionResult<VehicleResponse>> GetVehicle(int id)
        {
            var userId = (int)HttpContext.Items[UserIdKey]!;

            var vehicle = await _context
                .Vehicles.AsNoTracking()
                .Where(v => v.Id == id && v.UserId == userId)
                .ProjectToType<VehicleResponse>()
                .FirstOrDefaultAsync();

            if (vehicle == null)
            {
                return NotFound(new { message = "Vehículo no encontrado" });
            }

            return Ok(vehicle);
        }

        // POST: api/vehicles
        [HttpPost]
        [DisableRequestSizeLimit]
        public async Task<ActionResult<VehicleResponse>> CreateVehicle([FromForm] CreateVehicleRequest request, IFormFile? image)
        {
            var userId = (int)HttpContext.Items[UserIdKey]!;

            var existPlate = await _context.Vehicles.AnyAsync(v =>
                v.LicensePlate == request.LicensePlate
            );
            if (existPlate)
            {
                return BadRequest(new { message = "La placa ya existe" });
            }

            var vehicle = request.Adapt<Vehicle>();
            vehicle.UserId = userId;

            if (image != null && image.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "vehicles");
                Directory.CreateDirectory(uploadsFolder);

                var ext = Path.GetExtension(image.FileName);
                var fileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                await using (var stream = System.IO.File.Create(filePath))
                {
                    await image.CopyToAsync(stream);
                }

                vehicle.ImageUrl = $"/images/vehicles/{fileName}";
            }

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            var response = vehicle.Adapt<VehicleResponse>();
            return CreatedAtAction(nameof(GetVehicle), new { id = vehicle.Id }, response);
        }

        // PUT: api/vehicles/5
        [HttpPut("{id}")]
        [DisableRequestSizeLimit]
        public async Task<ActionResult<VehicleResponse>> UpdateVehicle(int id, [FromForm] UpdateVehicleRequest request, IFormFile? image)
        {
            var userId = (int)HttpContext.Items[UserIdKey]!;

            var vehicle = await _context.Vehicles
                .Where(v => v.UserId == userId)
                .FirstOrDefaultAsync(v => v.Id == id);

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

            if (image != null && image.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "vehicles");
                Directory.CreateDirectory(uploadsFolder);

                // delete old file if exists
                if (!string.IsNullOrEmpty(vehicle.ImageUrl))
                {
                    var oldRelative = vehicle.ImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                    var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", oldRelative);
                    if (System.IO.File.Exists(oldPath))
                    {
                        System.IO.File.Delete(oldPath);
                    }
                }

                var ext = Path.GetExtension(image.FileName);
                var fileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                await using (var stream = System.IO.File.Create(filePath))
                {
                    await image.CopyToAsync(stream);
                }

                vehicle.ImageUrl = $"/images/vehicles/{fileName}";
            }

            await _context.SaveChangesAsync();

            return Ok(vehicle.Adapt<VehicleResponse>());
        }

        // DELETE: api/vehicles/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVehicle(int id)
        {
            var userId = (int)HttpContext.Items[UserIdKey]!;

            var vehicle = await _context.Vehicles
                .Where(v => v.UserId == userId)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (vehicle == null)
            {
                return NotFound(new { message = "Vehículo no encontrado" });
            }

            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
