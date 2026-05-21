using GruYaApi.Data;
using GruYaApi.DTOs.Requests;
using GruYaApi.DTOs.Response;
using GruYaApi.DTOs.Responses;
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
    public class AuthController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly JwtTokenService _jwtTokenService;
        private readonly HashService _hashService;

        public AuthController(
            DataContext context,
            JwtTokenService jwtTokenService,
            HashService hashService
        )
        {
            _context = context;
            _jwtTokenService = jwtTokenService;
            _hashService = hashService;
        }

        // POST: api/auth/register
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            var existe = _context.Users.Any(u => u.Email == request.Email);
            if (existe)
                return BadRequest(new { message = "Email esta registrado" });
            var role = _context.Roles.FirstOrDefault(r => r.Id == request.RoleId);

            var nuevoUsuario = request.Adapt<User>();
            nuevoUsuario.Role = role!;
            nuevoUsuario.Password = _hashService.HashPassword(request.Password);

            _context.Users.Add(nuevoUsuario);
            await _context.SaveChangesAsync();

            var token = _jwtTokenService.GenerateToken(nuevoUsuario);
            return Ok(
                new AuthResponse { Token = token, User = nuevoUsuario.Adapt<UserResponse>() }
            );
        }

        // POST: api/auth/login
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var user = await _context
                .Users.Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || !_hashService.VerifyPassword(request.Password, user.Password))
                return Unauthorized(new { message = "Email o contraseña incorrectos" });

            var token = _jwtTokenService.GenerateToken(user);

            return Ok(new AuthResponse { Token = token, User = user.Adapt<UserResponse>() });
        }

        // POST: api/auth/logout
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // En JWT, el logout se maneja en el cliente eliminando el token
            return Ok(new { message = "Logout exitoso" });
        }
    }
}
