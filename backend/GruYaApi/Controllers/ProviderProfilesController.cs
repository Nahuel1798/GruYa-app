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
    public class ProviderProfilesController : ControllerBase
    {
        private readonly DataContext _context;

        public ProviderProfilesController(DataContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProviderProfileRequest request)
        {
            var existente = await _context.ProviderProfiles.FirstOrDefaultAsync(pp =>
                pp.UserId == request.UserId
            );
            if (existente != null)
                return BadRequest(
                    new { message = "Ya existe un perfil de proveedor para este usuario" }
                );

            var usuario = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId);

            if (usuario == null)
                return BadRequest(new { message = "El usuario no existe" });

            var providerProfile = request.Adapt<ProviderProfile>();

            providerProfile.User = usuario!;

            _context.ProviderProfiles.Add(providerProfile);
            _context.SaveChanges();

            return StatusCode(201, providerProfile);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProviderProfile(
            [FromBody] UpdateProviderProfileRequest request
        )
        {
            var existente = await _context
                .ProviderProfiles.Include(pp => pp.User)
                .Include(pp => pp.Location)
                .FirstOrDefaultAsync(pp => pp.UserId == request.UserId);
            if (existente == null)
                return BadRequest();

            var providerProfile = request.Adapt<ProviderProfile>();

            providerProfile.User = existente.User;
            providerProfile.Location.Id = existente.Location.Id;

            _context.SaveChanges();

            return Ok(providerProfile.Adapt<ProviderProfileResponse>());
        }

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
