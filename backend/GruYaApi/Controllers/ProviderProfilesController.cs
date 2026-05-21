using GruYaApi.Data;
using GruYaApi.DTOs.Requests;
using GruYaApi.DTOs.Response;
using GruYaApi.Models;
using GruYaApi.Service;
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
        private readonly JwtTokenService _jwtTokenService;
        private readonly HashService _hashService;

        public ProviderProfilesController(
            DataContext context,
            JwtTokenService jwtTokenService,
            HashService hashService
        )
        {
            _context = context;
            _jwtTokenService = jwtTokenService;
            _hashService = hashService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProviderProfileRequest request)
        {
            var usuario = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId);
            if (usuario == null)
                return BadRequest();

            var providerProfile = request.Adapt<ProviderProfile>();

            providerProfile.User = usuario!;

            _context.ProviderProfiles.Add(providerProfile);
            _context.SaveChanges();

            return StatusCode(201, providerProfile);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProviderProfile(
            [FromBody] UpdateProviderProfileRequest request
        )
        {
            var existente = await _context.ProviderProfiles.FirstOrDefaultAsync(pp =>
                pp.UserId == request.UserId
            );
            if (existente == null)
                return BadRequest();

            var providerProfile = request.Adapt<ProviderProfile>();

            // providerProfile = usuario!;

            _context.ProviderProfiles.Add(providerProfile);
            _context.SaveChanges();

            return StatusCode(201, providerProfile.Adapt<ProviderProfileResponse>());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOne(int id)
        {
            var providerProfile = await _context
                .ProviderProfiles.ProjectToType<ProviderProfileResponse>()
                .Include(pp => pp.User)
                .FirstOrDefaultAsync(pp => pp.Id == id);

            if (providerProfile == null)
                return NotFound();

            return StatusCode(201, providerProfile);
        }

        [HttpGet]
        public async Task<IActionResult> GetALl()
        {
            var providerProfile = await _context
                .ProviderProfiles.ProjectToType<ProviderProfileResponse>()
                .Include(pp => pp.User)
                .ToListAsync();

            return Ok(providerProfile);
        }
    }
}
