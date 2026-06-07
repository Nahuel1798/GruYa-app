using System.Security.Claims;
using GruYaApi.Data;
using GruYaApi.DTOs.Requests;
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

            var nuevoUsuario = request.Adapt<User>();
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
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || !_hashService.VerifyPassword(request.Password, user.Password))
                return Unauthorized(new { message = "Email o contraseña incorrectos" });

            var token = _jwtTokenService.GenerateToken(user);

            return Ok(new AuthResponse { Token = token, User = user.Adapt<UserResponse>() });
        }

        // GET: api/auth/profile

        [HttpGet("profile")]
        public async Task<IActionResult> Perfil()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var user = await _context
                .Users.ProjectToType<UserResponse>()
                .FirstOrDefaultAsync(u => u.Id.ToString() == userId);

            return Ok(user);
        }

        [HttpPut("role")]
        public async Task<IActionResult> EditRole(Role role)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "Usuario no encontrado" });

            user.Role = role;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Perfil actualizado exitosamente" });
        }

        // PUT: api/auth/editprofile
        [Authorize]
        [HttpPut("editprofile")]
        public async Task<IActionResult> EditProfile(UpdateUserRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "Usuario no encontrado" });

            var emailExists = await _context.Users.AnyAsync(u =>
                u.Email == request.Email && u.Id != user.Id
            );

            if (emailExists)
            {
                return BadRequest(new { message = "El email ya está registrado" });
            }

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.Email = request.Email;
            user.Phone = request.Phone;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Perfil actualizado exitosamente" });
        }

        // POST: api/auth/logout
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // En JWT, el logout se maneja en el cliente eliminando el token
            return Ok(new { message = "Logout exitoso" });
        }

        [HttpGet("validate")]
        public IActionResult ValidateJwt()
        {
            return Ok();
        }
    }
}
