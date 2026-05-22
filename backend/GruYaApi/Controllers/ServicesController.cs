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
    public class ServicesController : ControllerBase
    {
        private readonly DataContext _context;

        public ServicesController(DataContext context)
        {
            _context = context;
        }

        public static decimal DistanceInKm(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
        {
            const decimal R = 6371m;

            decimal dLat = DegreesToRadians(lat2 - lat1);
            decimal dLon = DegreesToRadians(lon2 - lon1);

            lat1 = DegreesToRadians(lat1);
            lat2 = DegreesToRadians(lat2);

            decimal a =
                (decimal)(Math.Sin((double)(dLat / 2)) * Math.Sin((double)(dLat / 2)))
                + (decimal)(
                    Math.Cos((double)lat1)
                    * Math.Cos((double)lat2)
                    * Math.Sin((double)(dLon / 2))
                    * Math.Sin((double)(dLon / 2))
                );

            decimal c = 2m * (decimal)Math.Atan2(Math.Sqrt((double)a), Math.Sqrt((double)(1 - a)));

            return R * c;
        }

        private static decimal DegreesToRadians(decimal degrees)
        {
            return degrees * (decimal)Math.PI / 180m;
        }

        [HttpPost("/provider/request")]
        public async Task<ActionResult<IEnumerable<VehicleResponse>>> RequestService(
            CreateServiceRequestRequest request
        )
        {
            var provider = _context
                .ProviderProfiles.Include(pp => pp.User)
                .FirstOrDefault(pp => pp.Id == request.ProviderId);

            var client = _context.Users.FirstOrDefault(u => u.Id == request.ClientId);

            var vehicle = _context.Vehicles.FirstOrDefault(v => v.Id == request.VehicleId);

            if (provider == null)
                return NotFound();
            var newService = request.Adapt<ServiceRequest>();
            newService.Provider = provider.User;
            newService.Client = client;
            newService.Vehicle = vehicle;
            _context.ServiceRequests.Add(newService);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("/provider")]
        public async Task<ActionResult<VehicleResponse>> GetWithProviderDefined()
        {
            return Ok();
        }

        [HttpGet("{lat}/{lon}/{range}")]
        public async Task<ActionResult<VehicleResponse>> ListRanges(
            decimal lat,
            decimal lon,
            decimal range
        )
        {
            var services = await _context.ServiceRequests.Include(sr => sr.Location).ToListAsync();
            Console.WriteLine(services.Count);

            foreach (ServiceRequest item in services)
            {
                Console.WriteLine(item.Id);
                Console.WriteLine(
                    DistanceInKm(item.Location.Latitude, item.Location.Longitude, lat, lon)
                );
            }

            return Ok();
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

            return Ok(new { message = "Vehículo actualizado correctamente" });
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
