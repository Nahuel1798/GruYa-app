using GruYaApi.Data;
using GruYaApi.DTOs.Requests;
using GruYaApi.DTOs.Responses;
using GruYaApi.Models;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GruYaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProviderProfilesController : ControllerBase
    {
        private readonly DataContext _context;

        public ProviderProfilesController(DataContext context)
        {
            _context = context;
        }

        // POST: api/ProviderProfiles
        // Crea un nuevo perfil de proveedor, verificando que no exista un perfil para el mismo usuario y que el usuario exista antes de crear el perfil.

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProviderProfileRequest request)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return BadRequest(new { message = "Identificador de usuario inválido" });

            var existente = await _context.ProviderProfiles.FirstOrDefaultAsync(pp => pp.UserId == userId);
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

            return StatusCode(201, providerProfile);
        }

        // PUT: api/ProviderProfiles
        // Actualiza un perfil de proveedor existente, verificando que el perfil exista antes de actualizarlo y manteniendo la relación con el usuario y la ubicación.
        [HttpPut]
        public async Task<IActionResult> UpdateProviderProfile(
            [FromBody] UpdateProviderProfileRequest request)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return BadRequest(new { message = "Identificador de usuario inválido" });

            var existente = await _context
                .ProviderProfiles
                .Include(pp => pp.User)
                .Include(pp => pp.Location)
                .FirstOrDefaultAsync(pp => pp.UserId == request.UserId);

            if (existente == null)
            {
                return NotFound(new
                {
                    message = "Perfil de proveedor no encontrado"
                });
            }

            request.Adapt(existente);

            await _context.SaveChangesAsync();

            return Ok(existente.Adapt<ProviderProfileResponse>());
        }

        // GET: api/ProviderProfiles/5
        // Obtiene un perfil de proveedor específico por su ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOne(int id)
        {
            var providerProfile = await _context
                .ProviderProfiles.Include(pp => pp.User)
                .ProjectToType<ProviderProfileResponse>()
                .FirstOrDefaultAsync(pp => pp.Id == id);

            if (providerProfile == null)
                return NotFound();

            return Ok(providerProfile);
        }

        // GET: api/ProviderProfiles/user/5
        // Obtiene un perfil de proveedor específico por el ID del usuario
        [HttpGet]
        public async Task<IActionResult> GetALl()
        {
            var providerProfile = await _context
                .ProviderProfiles.Include(pp => pp.User)
                .ProjectToType<ProviderProfileResponse>()
                .ToListAsync();

            return Ok(providerProfile);
        }
    }
}
