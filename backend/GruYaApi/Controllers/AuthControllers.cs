using GruYaApi.Data;
using GruYaApi.DTOs.Response;
using GruYaApi.Models;
using GruYaApi.DTOs.Request;
using GruYaApi.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mapster;
using GruYaApi.DTOs.Requests;
using Microsoft.EntityFrameworkCore;
using GruYaApi.DTOs.Responses;

namespace GruYaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AuthControllers : ControllerBase
    {
        private readonly DataContext _context;
        private readonly JwtTokenService _jwtTokenService;
        private readonly HashService _hashService;

        public AuthControllers(
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
            nuevoUsuario.Password = _hashService.HashPassword(request.Contrasena);

            _context.Users.Add(nuevoUsuario);
            await _context.SaveChangesAsync();

            var token = _jwtTokenService.GenerateToken(nuevoUsuario);
             return Ok(new AuthResponse
            {
                Token = token,
                Usuario = nuevoUsuario.Adapt<UserResponse>()
            });
        }

        // POST: api/auth/login
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null || !_hashService.VerifyPassword(request.Contrasena, user.Password))
                return Unauthorized(new { message = "Email o contraseña incorrectos" });
            var token = _jwtTokenService.GenerateToken(user);
            return Ok(new AuthResponse
            {
                Token = token,
                Usuario = user.Adapt<UserResponse>()
            });
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