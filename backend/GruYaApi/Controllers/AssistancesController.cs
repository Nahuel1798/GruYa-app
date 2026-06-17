using GruYaApi.Data;
using GruYaApi.DTOs.Requests;
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
    public class AssistancesController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly OsrmService _osrmService;

        public AssistancesController(DataContext context, OsrmService osrmService)
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

        /*

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

        */

        // POST: api/assistances/request
        // Crea una solicitud de auxilio. Si se especifica un providerId, se dirige a ese proveedor.
        // Si no, la solicitud queda abierta para que cualquier proveedor pueda cotizar.
        [HttpPost("request")]
        public async Task<IActionResult> RequestAssistance(
            [FromBody] CreateAssistanceRequest request
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

            int? providerProfileId = null;

            if (request.ProviderId.HasValue)
            {
                var providerProfile = await _context
                    .ProviderProfiles.Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.Id == request.ProviderId.Value && p.IsAvailable);

                if (providerProfile == null)
                {
                    return Conflict(new { Message = "El prestador solicitado no está disponible" });
                }

                providerProfileId = providerProfile.Id;
            }

            // Calcular ruta
            double? distanceKm = null;
            double? etaMinutes = null;
            string? routeGeometry = null;

            try
            {
                var route = await _osrmService.GetRouteInfoAsync(
                    request.Origin.Latitude,
                    request.Origin.Longitude,
                    request.Destination.Latitude,
                    request.Destination.Longitude
                );

                distanceKm = route.DistanceKm;
                etaMinutes = route.EtaMinutes;
                routeGeometry = route.GeometryJson;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculando ruta: {ex.Message}");
            }

            var assistance = new Assistance
            {
                ServiceType = request.ServiceType,
                IssueType = request.IssueType,
                Status = AssistanceStatus.Pendiente,
                Client = client,
                Vehicle = vehicle,
                Origin = request.Origin,
                Destination = request.Destination,
                RequestedProviderProfileId = providerProfileId,

                // Nuevos campos
                DistanceKm = distanceKm,
                EtaMinutes = etaMinutes,
                RouteGeometry = routeGeometry,
            };

            _context.Assistances.Add(assistance);

            await _context.SaveChangesAsync();

            var response = assistance.Adapt<AssistanceResponse>();
            return Ok(response);
        }

        // GET: api/assistances/{id}
        // Obtiene los detalles de una solicitud de auxilio
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAssistance(int id)
        {
            var assistance = await _context
                .Assistances.Include(a => a.Origin)
                .Include(a => a.Destination)
                .Include(a => a.Client)
                .Include(a => a.Provider)
                .Include(a => a.Vehicle)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assistance == null)
            {
                return NotFound(new { Message = "Asistencia no encontrada" });
            }

            var response = assistance.Adapt<AssistanceResponse>();

            if (assistance.Provider != null)
            {
                response.ProviderProfile = await _context
                    .ProviderProfiles
                    .AsNoTracking()
                    .Where(pp => pp.UserId == assistance.Provider.Id)
                    .ProjectToType<ProviderProfileResponse>()
                    .FirstOrDefaultAsync();
            }

            return Ok(response);
        }

        // GET: api/assistances/my
        // Devuelve todas las solicitudes de auxilio del usuario autenticado
        // Opcional: ?status=Pendiente para filtrar por estado
        [HttpGet("my")]
        public async Task<IActionResult> GetMyAssistances(AssistanceStatus? status = null)
        {
            var idUsuario = (int)HttpContext.Items["idUsuario"]!;

            var query = _context
                .Assistances.Include(a => a.Origin)
                .Include(a => a.Destination)
                .Include(a => a.Vehicle)
                .Include(a => a.Provider)
                .Where(a => a.Client.Id == idUsuario)
                .AsNoTracking();

            if (status.HasValue)
            {
                query = query.Where(a => a.Status == status.Value);
            }

            var assistances = await query
                .OrderByDescending(a => a.Id)
                .ToListAsync();

            var response = assistances.Select(a =>
            {
                var dto = a.Adapt<AssistanceResponse>();

                if (a.Provider != null)
                {
                    dto.ProviderProfile = _context
                        .ProviderProfiles
                        .AsNoTracking()
                        .Where(pp => pp.UserId == a.Provider.Id)
                        .ProjectToType<ProviderProfileResponse>()
                        .FirstOrDefault();
                }

                return dto;
            });

            return Ok(response);
        }

        // GET: api/assistances/assistance-nearby
        // Obtiene solicitudes de auxilio (Assistance) cercanas a un proveedor, ordenadas por distancia
        // Devuelve tanto solicitudes abiertas como las dirigidas al proveedor autenticado
        [HttpGet("assistance-nearby")]
        public async Task<IActionResult> NearbyAssistance(decimal rangekm = 20)
        {
            var userId = (int)HttpContext.Items["idUsuario"]!;

            var provider = await _context.ProviderProfiles.FirstOrDefaultAsync(p =>
                p.UserId == userId
            );

            if (provider == null)
                return NotFound(new { Message = "Perfil de proveedor no encontrado" });

            var profileIds = await _context
                .ProviderProfiles.Where(p => p.UserId == userId)
                .Select(p => p.Id)
                .ToListAsync();

            // Abiertas — sin proveedor asignado, sin quote pendiente del caller
            var openRequests = await _context
                .Assistances.Include(r => r.Client)
                .Include(r => r.Vehicle)
                .Where(r =>
                    r.Status == AssistanceStatus.Pendiente
                    && r.Provider == null
                    && r.RequestedProviderProfileId == null
                    && !_context.Quotes.Any(q =>
                        q.AssistanceId == r.Id
                        && profileIds.Contains(q.ProviderProfileId)
                        && q.Status == QuoteStatus.Pendiente
                    )
                )
                .ToListAsync();

            // Dirigidas — apuntan a algún profile del caller
            var directedRequests = await _context
                .Assistances.Include(r => r.Client)
                .Include(r => r.Vehicle)
                .Where(r =>
                    r.Status == AssistanceStatus.Pendiente
                    && r.Provider == null
                    && r.RequestedProviderProfileId != null
                    && profileIds.Contains(r.RequestedProviderProfileId.Value)
                )
                .ToListAsync();

            var providerLat = provider.Location.Latitude;
            var providerLon = provider.Location.Longitude;

            static decimal Haversine(decimal lat1, decimal lon1, decimal lat2, decimal lon2) =>
                DistanceInKm(lat1, lon1, lat2, lon2);

            var openResult = openRequests
                .Where(r =>
                    Haversine(providerLat, providerLon, r.Origin.Latitude, r.Origin.Longitude)
                    <= rangekm
                )
                .Select(r => new NearbyAssistanceResponse
                {
                    Id = r.Id,
                    ServiceType = r.ServiceType.ToString(),
                    IssueType = r.IssueType.ToString(),
                    ClientName = $"{r.Client.FirstName} {r.Client.LastName}",
                    Vehicle = $"{r.Vehicle.Brand} {r.Vehicle.Model}",
                    Origin = r.Origin,
                    Destination = r.Destination,
                    DistanceKm = Math.Round(
                        Haversine(providerLat, providerLon, r.Origin.Latitude, r.Origin.Longitude),
                        2
                    ),
                    IsDirected = false,
                });

            var directedResult = directedRequests
                .Where(r =>
                    Haversine(providerLat, providerLon, r.Origin.Latitude, r.Origin.Longitude)
                    <= rangekm
                )
                .Select(r => new NearbyAssistanceResponse
                {
                    Id = r.Id,
                    ServiceType = r.ServiceType.ToString(),
                    IssueType = r.IssueType.ToString(),
                    ClientName = $"{r.Client.FirstName} {r.Client.LastName}",
                    Vehicle = $"{r.Vehicle.Brand} {r.Vehicle.Model}",
                    Origin = r.Origin,
                    Destination = r.Destination,
                    DistanceKm = Math.Round(
                        Haversine(providerLat, providerLon, r.Origin.Latitude, r.Origin.Longitude),
                        2
                    ),
                    IsDirected = true,
                });

            var result = openResult.Concat(directedResult).OrderBy(r => r.DistanceKm).ToList();

            return Ok(result);
        }

        // GET: api/assistances/providers-nearby?latitude=-33.3&longitude=-66.3&rangeKm=20
        [HttpGet("providers-nearby")]
        public async Task<ActionResult<List<ProviderLocationResponse>>> NearbyProviders(
            decimal latitude,
            decimal longitude,
            decimal rangeKm = 20
        )
        {
            var providers = await _context
                .ProviderProfiles.Include(p => p.User)
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

        // GET: api/assistances/nearby
        // Obtiene una lista de solicitudes de servicio cercanas a una ubicación específica, filtrando por la distancia y ordenando por la distancia más cercana. La función utiliza la fórmula de Havers
        [HttpGet("nearby")]
        public async Task<IActionResult> NearbyServices(
            decimal latitude,
            decimal longitude,
            decimal rangeKm = 20
        )
        {
            var services = await _context.Assistances.Include(s => s.Client).ToListAsync();

            var result = services
                .Where(s =>
                    DistanceInKm(latitude, longitude, s.Origin.Latitude, s.Origin.Longitude)
                    <= rangeKm
                )
                .Select(s => new
                {
                    s.Id,
                    s.ServiceType,
                    OriginLatitude = s.Origin.Latitude,
                    OriginLongitude = s.Origin.Longitude,
                });

            return Ok(result);
        }

        [HttpGet("{lat}/{lon}/{range}")]
        public async Task<ActionResult> ListRanges(decimal lat, decimal lon, decimal range)
        {
            var services = await _context.Assistances.ToListAsync();
            Console.WriteLine(services.Count);

            foreach (Assistance item in services)
            {
                Console.WriteLine(item.Id);
                Console.WriteLine(
                    DistanceInKm(item.Origin.Latitude, item.Origin.Longitude, lat, lon)
                );
            }

            return Ok();
        }
    }
}
