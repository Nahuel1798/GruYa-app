using System.Security.Claims;
using GruYaApi.Data;
using GruYaApi.DTOs.Requests;
using GruYaApi.DTOs.Responses;
using GruYaApi.Filters;
using GruYaApi.Models;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GruYaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ServiceFilter(typeof(UserExists))]
    public class ProviderProfilesController : ControllerBase
    {
        private readonly DataContext _context;
        private const string UserIdKey = "idUsuario";

        public ProviderProfilesController(DataContext context)
        {
            _context = context;
        }

        // POST: api/ProviderProfiles
        // Crea un nuevo perfil de proveedor, verificando que no exista un perfil para el mismo usuario y que el usuario exista antes de crear el perfil.

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProviderProfileRequest request)
        {
            var userIdClaim = User
                .Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)
                ?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return BadRequest(new { message = "Identificador de usuario inválido" });

            var existente = await _context.ProviderProfiles.FirstOrDefaultAsync(pp =>
                pp.UserId == userId
            );
            if (existente != null)
                return BadRequest(
                    new { message = "Ya existe un perfil de proveedor para este usuario" }
                );

            var usuario = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (usuario == null)
                return BadRequest(new { message = "El usuario no existe" });

            var providerProfile = request.Adapt<ProviderProfile>();

            providerProfile.User = usuario!;

            _context.ProviderProfiles.Add(providerProfile);
            await _context.SaveChangesAsync();

            return StatusCode(201, providerProfile.Adapt<ProviderProfileResponse>());
        }

        // PUT: api/ProviderProfiles
        // Actualiza un perfil de proveedor existente, verificando que el perfil exista antes de actualizarlo y manteniendo la relación con el usuario y la ubicación.
        [HttpPut]
        public async Task<IActionResult> UpdateProviderProfile(
            [FromBody] UpdateProviderProfileRequest request
        )
        {
            var userId = (int)HttpContext.Items[UserIdKey!];

            var existente = await _context
                .ProviderProfiles.Include(pp => pp.User)
                .FirstOrDefaultAsync(pp => pp.UserId == userId);

            if (existente == null)
            {
                return NotFound(new { message = "Perfil de proveedor no encontrado" });
            }
            var currentLocation = existente.Location;

            request.Adapt(existente);

            if (request.Location == null)
            {
                existente.Location = currentLocation;
            }
            await _context.SaveChangesAsync();

            return Ok(existente.Adapt<ProviderProfileResponse>());
        }

        // GET: api/ProviderProfiles
        // Lista todos los perfiles de proveedor
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var profiles = await _context
                .ProviderProfiles.AsNoTracking()
                .Include(pp => pp.User)
                .ProjectToType<ProviderProfileResponse>()
                .ToListAsync();

            return Ok(profiles);
        }

        // GET: api/ProviderProfiles/me
        // Obtiene el perfil del proveedor logueado
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var idUsuario = (int)HttpContext.Items["idUsuario"]!;
            var profile = await _context
                .ProviderProfiles.AsNoTracking()
                .Include(pp => pp.User)
                .ProjectToType<ProviderProfileResponse>()
                .FirstOrDefaultAsync(pp => pp.User.Id == idUsuario);

            if (profile == null)
                return NotFound(new { message = "Perfil de proveedor no encontrado" });

            return Ok(profile);
        }

        // GET: api/ProviderProfiles/5
        // Obtiene un perfil específico por ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var profile = await _context
                .ProviderProfiles.AsNoTracking()
                .Include(pp => pp.User)
                .ProjectToType<ProviderProfileResponse>()
                .FirstOrDefaultAsync(pp => pp.Id == id);

            if (profile == null)
                return NotFound(new { message = "Perfil de proveedor no encontrado" });

            return Ok(profile);
        }

        [HttpPatch("location")]
        public async Task<IActionResult> UpdateProviderLocation([FromBody] Location location)
        {
            var userId = (int)HttpContext.Items[UserIdKey]!;

            var providerProfile = await _context.ProviderProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

            if (providerProfile == null)
                return NotFound(new { Message = "Perfil de proveedor no encontrado" });

            providerProfile.CurrentLocation ??= new Location();
            providerProfile.CurrentLocation.Latitude = location.Latitude;
            providerProfile.CurrentLocation.Longitude = location.Longitude;
            providerProfile.LastLocationUpdate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Ubicación actualizada",
                Latitude = providerProfile.CurrentLocation?.Latitude,
                Longitude = providerProfile.CurrentLocation?.Longitude,
                LastLocationUpdate = providerProfile.LastLocationUpdate,
            });
        }
    }
}
