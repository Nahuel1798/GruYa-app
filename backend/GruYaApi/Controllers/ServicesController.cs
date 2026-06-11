using GruYaApi.Data;
using GruYaApi.DTOs.Requests;
using GruYaApi.DTOs.Response;
using GruYaApi.DTOs.Responses;
using GruYaApi.Filters;
using GruYaApi.Models;
using GruYaApi.Services;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GruYaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [ServiceFilter(typeof(UserExists))]
    public class ServicesController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly OsrmService _osrmService;

        public ServicesController(DataContext context, OsrmService osrmService)
        {
            _context = context;
            _osrmService = osrmService;
        }

        // Función para calcular la distancia entre dos puntos geográficos utilizando la fórmula de Haversine

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

        // POST: api/services/request
        // Crea una nueva solicitud de servicio, asignando automáticamente el proveedor más cercano disponible según la ubicación del cliente y el vehículo, y calculando la distancia y el tiempo estimado de llegada
        [HttpPost("request_old")]
        public async Task<IActionResult> RequestService(
            [FromBody] CreateServiceRequestRequest request
        )
        {
            var idUsuario = (int)HttpContext.Items["idUsuario"]!;
            var client = await _context.Users.FirstOrDefaultAsync(u => u.Id == idUsuario);

            if (client == null)
                return NotFound(new { Message = "Cliente no encontrado" });

            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v =>
                v.Id == request.VehicleId
            );

            if (vehicle == null)
                return NotFound(new { Message = "Vehículo no encontrado" });

            var providers = await _context
                .ProviderProfiles.Include(p => p.User)
                .Where(p => p.IsAvailable)
                .ToListAsync();

            if (!providers.Any())
                return BadRequest(new { Message = "No hay proveedores disponibles" });

            ProviderProfile? bestProvider = null;
            double bestDistance = double.MaxValue;
            double bestEta = double.MaxValue;

            foreach (var provider in providers)
            {
                try
                {
                    var route = await _osrmService.GetRouteInfoAsync(
                        request.Location.Latitude,
                        request.Location.Longitude,
                        provider.Location.Latitude,
                        provider.Location.Longitude
                    );

                    if (route.DistanceKm < bestDistance)
                    {
                        bestProvider = provider;
                        bestDistance = route.DistanceKm;
                        bestEta = route.EtaMinutes;
                    }
                }
                catch
                {
                    continue;
                }
            }

            if (bestProvider == null)
                return BadRequest(new { Message = "No fue posible encontrar una grúa" });

            var location = request.Location.Adapt<Location>();

            _context.Locations.Add(location);

            var serviceRequest = new ServiceRequest
            {
                ServiceType = request.ServiceType,
                Client = client,
                Provider = bestProvider.User,
                Vehicle = vehicle,
                Location = location,
            };

            _context.ServiceRequests.Add(serviceRequest);

            await _context.SaveChangesAsync();

            return Ok(
                new
                {
                    ServiceRequestId = serviceRequest.Id,
                    ProviderId = bestProvider.Id,
                    ProviderName = bestProvider.User.FirstName + " " + bestProvider.User.LastName,
                    DistanceKm = Math.Round(bestDistance, 2),
                    EtaMinutes = Math.Round(bestEta),
                }
            );
        }

        // POST: api/services/request
        // Crea una solicitud de auxilio. Si se especifica un providerId, asigna ese proveedor.
        // Si no, la solicitud queda sin asignar para que los proveedores cercanos puedan aceptarla.
        [HttpPost("request")]
        public async Task<IActionResult> RequestAssistance(
            [FromBody] CreateServiceRequestRequest request
        )
        {
            var idUsuario = (int)HttpContext.Items["idUsuario"]!;
            var client = await _context.Users.FirstOrDefaultAsync(u => u.Id == idUsuario);
            if (client == null)
                return NotFound(new { Message = "Cliente no encontrado" });

            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v =>
                v.Id == request.VehicleId
            );
            if (vehicle == null)
                return NotFound(new { Message = "Vehículo no encontrado" });

            User? provider = null;

            if (request.ProviderId.HasValue)
            {
                var providerProfile = await _context
                    .ProviderProfiles.Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.Id == request.ProviderId.Value && p.IsAvailable);

                if (providerProfile == null)
                    return Conflict(
                        new { Message = "El prestador solicitado no está disponible" }
                    );

                provider = providerProfile.User;
            }

            var location = request.Location.Adapt<Location>();
            _context.Locations.Add(location);

            var serviceRequest = new ServiceRequest
            {
                ServiceType = request.ServiceType,
                IssueType = request.IssueType,
                Status = ServiceRequestStatus.Pendiente,
                Client = client,
                Provider = provider,
                Vehicle = vehicle,
                Location = location,
            };

            _context.ServiceRequests.Add(serviceRequest);
            await _context.SaveChangesAsync();

            return Ok(
                new
                {
                    ServiceRequestId = serviceRequest.Id,
                    HasProvider = provider != null,
                }
            );
        }

        // GET: api/services/providers-nearby?latitude=-33.3&longitude=-66.3&rangeKm=20
        [HttpGet("providers-nearby")]
        public async Task<ActionResult<List<ProviderLocationResponse>>> NearbyProviders(
            decimal latitude,
            decimal longitude,
            decimal rangeKm = 20
        )
        {
            var providers = await _context
                .ProviderProfiles
                .Include(p => p.User)
                .Include(p => p.Location)
                .Where(p => p.IsAvailable)
                .ToListAsync();

            var result = providers
                .Where(p =>
                    DistanceInKm(latitude, longitude, p.Location.Latitude, p.Location.Longitude)
                    <= rangeKm
                )
                .Select(p => new ProviderLocationResponse
                {
                    Id = p.Id,
                    UserId = p.UserId,
                    CompanyName = p.CompanyName,
                    Phone = p.User.Phone,
                    Description = p.Description,
                    ServiceType = p.ServiceType,
                    Latitude = p.Location.Latitude,
                    Longitude = p.Location.Longitude,
                    IsAvailable = p.IsAvailable,
                })
                .ToList();

            return Ok(result);
        }

        // GET: api/nearby
        // Obtiene una lista de solicitudes de servicio cercanas a una ubicación específica, filtrando por la distancia y ordenando por la distancia más cercana. La función utiliza la fórmula de Havers
        [HttpGet("nearby")]
        public async Task<IActionResult> NearbyServices(
            decimal latitude,
            decimal longitude,
            decimal rangeKm = 20
        )
        {
            var services = await _context
                .ServiceRequests.Include(s => s.Location)
                .Include(s => s.Client)
                .ToListAsync();

            var result = services
                .Where(s =>
                    DistanceInKm(latitude, longitude, s.Location.Latitude, s.Location.Longitude)
                    <= rangeKm
                )
                .Select(s => new
                {
                    s.Id,
                    s.ServiceType,
                    Latitude = s.Location.Latitude,
                    Longitude = s.Location.Longitude,
                });

            return Ok(result);
        }

        // GET: api/provider
        // Obtiene una lista de solicitudes de servicio para un proveedor específico, filtrando por el estado de la solicitud y ordenando por fecha de creación.

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
        // Actualiza los detalles de un vehículo específico, verificando que la patente no se duplique con otro vehículo existente.

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
        // Elimina un vehículo específico, verificando que el vehículo exista antes de eliminarlo.

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
