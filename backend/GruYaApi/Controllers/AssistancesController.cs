using GruYaApi.Data;
using GruYaApi.DTOs.Requests;
using GruYaApi.DTOs.Responses;
using GruYaApi.Filters;
using GruYaApi.Models;
using GruYaApi.Service;
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
        private readonly INotificationService? _notificationService;

        public AssistancesController(
            DataContext context,
            OsrmService osrmService,
            INotificationService? notificationService
        )
        {
            _context = context;
            _osrmService = osrmService;
            _notificationService = notificationService;
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

            // Verificar si el cliente ya tiene una solicitud activa
            var activeStatuses = new[] { AssistanceStatus.Pendiente, AssistanceStatus.EnProceso };

            var hasActiveAssistance = await _context.Assistances.AnyAsync(a =>
                a.ClientId == client.Id && activeStatuses.Contains(a.Status)
            );

            if (hasActiveAssistance)
            {
                return Conflict(new { Message = "Ya tienes una solicitud de auxilio activa" });
            }

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

                DistanceKm = distanceKm,
                EtaMinutes = etaMinutes,
                RouteGeometry = routeGeometry,
            };

            _context.Assistances.Add(assistance);

            await _context.SaveChangesAsync();

            // Notify providers (fire-and-forget: NotificationService never throws)
            if (_notificationService is not null)
            {
                if (providerProfileId.HasValue)
                {
                    // Directed: notify specific provider
                    var provider = await _context
                        .ProviderProfiles.Include(p => p.User)
                        .FirstOrDefaultAsync(p => p.Id == providerProfileId.Value);

                    if (provider != null)
                    {
                        await _notificationService.SendToUserAsync(
                            provider.UserId,
                            "Te han solicitado un servicio",
                            $"{request.ServiceType} - {request.IssueType}",
                            new Dictionary<string, string>
                            {
                                ["type"] = "directed_assistance",
                                ["assistanceId"] = assistance.Id.ToString(),
                                ["serviceType"] = request.ServiceType.ToString(),
                                ["issueType"] = request.IssueType.ToString(),
                            }
                        );
                    }
                }
                else
                {
                    // Open: notify nearby available providers
                    var nearbyProviders = await _context
                        .ProviderProfiles.Include(p => p.User)
                        .Where(p => p.IsAvailable)
                        .ToListAsync();

                    var matchingTokens = nearbyProviders
                        .Where(p =>
                            DistanceInKm(
                                request.Origin.Latitude,
                                request.Origin.Longitude,
                                p.Location.Latitude,
                                p.Location.Longitude
                            ) <= 20m
                            && !string.IsNullOrWhiteSpace(p.User.FcmToken)
                        )
                        .Select(p => p.User.FcmToken!)
                        .ToList();

                    if (matchingTokens.Count > 0)
                    {
                        await _notificationService.SendToMultipleAsync(
                            matchingTokens,
                            "Nueva solicitud de auxilio cerca",
                            $"Tipo: {request.ServiceType}",
                            new Dictionary<string, string>
                            {
                                ["type"] = "new_assistance",
                                ["assistanceId"] = assistance.Id.ToString(),
                                ["serviceType"] = request.ServiceType.ToString(),
                                ["issueType"] = request.IssueType.ToString(),
                                ["originLat"] = request.Origin.Latitude.ToString(),
                                ["originLon"] = request.Origin.Longitude.ToString(),
                            }
                        );
                    }
                }
            }

            var response = assistance.Adapt<AssistanceResponse>();

            return Ok(response);
        }

        // GET: api/assistances/active
        // Devuelve la solicitud de auxilio activa del usuario autenticado, si existe.
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveAssistance()
        {
            var idUsuario = (int)HttpContext.Items["idUsuario"]!;

            var assistance = await _context
                .Assistances.Include(a => a.Origin)
                .Include(a => a.Destination)
                .Include(a => a.Vehicle)
                .FirstOrDefaultAsync(a =>
                    a.ClientId == idUsuario
                    && a.Status != AssistanceStatus.Completado
                    && a.Status != AssistanceStatus.Cancelado
                );

            if (assistance == null)
                return NotFound();

            return Ok(assistance.Adapt<AssistanceResponse>());
        }

        // PUT: api/assistances/{id}/start-trip
        // Permite al proveedor asignado iniciar el viaje (cambia estado a EnProceso y notifica al cliente)
        [HttpPut("{id}/start-trip")]
        public async Task<IActionResult> StartTrip(int id)
        {
            var userId = (int)HttpContext.Items["idUsuario"]!;

            var assistance = await _context
                .Assistances.Include(a => a.Client)
                .Include(a => a.Provider)
                .Include(a => a.Vehicle)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assistance == null)
                return NotFound(new { Message = "Asistencia no encontrada" });

            // Verificar que el usuario autenticado es el proveedor asignado
            if (assistance.Provider == null || assistance.Provider.Id != userId)
                return Forbid();

            // Verificar que la asistencia está en estado válido para iniciar
            if (assistance.Status == AssistanceStatus.Completado || assistance.Status == AssistanceStatus.Cancelado)
                return Conflict(new { Message = "La asistencia ya ha finalizado" });

            if (assistance.Status == AssistanceStatus.EnProceso)
                return Conflict(new { Message = "El viaje ya ha sido iniciado" });

            // Cambiar estado a EnProceso
            assistance.Status = AssistanceStatus.EnProceso;
            await _context.SaveChangesAsync();

            // Session ID for SignalR location tracking
            var trackingSessionId = $"assistance-{assistance.Id}";

            // Notificar al cliente que el proveedor ha iniciado el viaje
            if (_notificationService is not null)
            {
                await _notificationService.SendToUserAsync(
                    assistance.ClientId,
                    "Tu proveedor ha iniciado el viaje",
                    "El proveedor está en camino hacia tu ubicación",
                    new Dictionary<string, string>
                    {
                        ["type"] = "trip_started",
                        ["assistanceId"] = assistance.Id.ToString(),
                        ["providerId"] = assistance.Provider!.Id.ToString(),
                        ["trackingSessionId"] = trackingSessionId,
                    });
            }

            var response = new TripStartedResponse
            {
                IdAssistance = assistance.Id,
                TrackingSessionId = trackingSessionId
            };
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
                    .ProviderProfiles.AsNoTracking()
                    .Where(pp => pp.UserId == assistance.Provider.Id)
                    .ProjectToType<ProviderProfileResponse>()
                    .FirstOrDefaultAsync();
            }

            return Ok(response);
        }

        [HttpPatch("active/cancel")]
        public async Task<IActionResult> CancelAssistance()
        {
            var idUsuario = (int)HttpContext.Items["idUsuario"]!;

            var assistance = await _context.Assistances.FirstOrDefaultAsync(a =>
                a.ClientId == idUsuario
                && a.Status != AssistanceStatus.Completado
                && a.Status != AssistanceStatus.Cancelado
            );

            if (assistance == null)
                return NotFound();
            if (assistance == null)
            {
                return NotFound(new { Message = "No se encontró una asistencia activa" });
            }
            assistance.Status = AssistanceStatus.Cancelado;
            await _context.SaveChangesAsync();

            return Ok();
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

            var assistances = await query.OrderByDescending(a => a.Id).ToListAsync();

            var response = assistances.Select(a =>
            {
                var dto = a.Adapt<AssistanceResponse>();

                if (a.Provider != null)
                {
                    dto.ProviderProfile = _context
                        .ProviderProfiles.AsNoTracking()
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
        // Obtiene una lista de proveedores cercanos a una ubicación específica, filtrando por la distancia.
        // Devuelve información básica del proveedor y su ubicación.
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
        // Obtiene una lista de solicitudes de servicio cercanas a una ubicación específica,
        // filtrando por la distancia y ordenando por la distancia más cercana. La función utiliza la fórmula de Havers
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
