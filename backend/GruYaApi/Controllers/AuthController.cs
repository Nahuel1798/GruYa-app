using System.Security.Claims;
using FirebaseAdmin.Auth;
using GruYaApi.Data;
using GruYaApi.DTOs.Request;
using GruYaApi.DTOs.Requests;
using GruYaApi.DTOs.Responses;
using GruYaApi.Models;
using GruYaApi.Filters;
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
        private const string UserIdKey = "idUsuario";

        private static string NormalizeEmail(string? email)
        {
            return email?.Trim().ToLowerInvariant() ?? string.Empty;
        }

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
            if (request.Role != Role.User && request.Role != Role.Provider)
                return BadRequest(new { message = "Rol inválido. Solo se permiten los roles User y Provider" });

            var normalizedEmail = NormalizeEmail(request.Email);

            var existe = await _context.Users.AnyAsync(u => u.Email == normalizedEmail);
            if (existe)
                return BadRequest(new { message = "Email esta registrado" });

            var nuevoUsuario = request.Adapt<User>();
            nuevoUsuario.Email = normalizedEmail;
            nuevoUsuario.Password = _hashService.HashPassword(request.Password);

            _context.Users.Add(nuevoUsuario);
            await _context.SaveChangesAsync();

            return Ok(await GenerateAuthResponse(nuevoUsuario));
        }

        // POST: api/auth/login
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var normalizedEmail = NormalizeEmail(request.Email);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

            if (user == null || !_hashService.VerifyPassword(request.Password, user.Password))
                return Unauthorized(new { message = "Email o contraseña incorrectos" });

            if (!string.IsNullOrWhiteSpace(request.FcmToken))
            {
                user.FcmToken = request.FcmToken;
            }

            return Ok(await GenerateAuthResponse(user));
        }

        // POST: api/auth/google-login
        [AllowAnonymous]
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.IdToken))
                return BadRequest(new { message = "El token de Google es requerido" });

            try
            {
                var firebaseToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(request.IdToken);

                var email = NormalizeEmail(
                    firebaseToken.Claims.TryGetValue("email", out var emailClaim)
                        ? emailClaim?.ToString()
                        : null
                );

                if (string.IsNullOrWhiteSpace(email))
                    return Unauthorized(new { message = "No se pudo obtener el email del token de Google" });

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    var fullName = firebaseToken.Claims.TryGetValue("name", out var nameClaim)
                        ? nameClaim?.ToString()
                        : null;
                    var picture = firebaseToken.Claims.TryGetValue("picture", out var pictureClaim)
                        ? pictureClaim?.ToString()
                        : null;

                    var nameParts = (fullName ?? email.Split('@')[0]).Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

                    user = new User
                    {
                        FirstName = nameParts.Length > 0 ? nameParts[0] : "Google",
                        LastName = nameParts.Length > 1 ? nameParts[1] : "User",
                        Email = email,
                        Password = _hashService.HashPassword(Guid.NewGuid().ToString("N")),
                        Role = Role.User,
                        AvatarUrl = picture,
                    };

                    _context.Users.Add(user);
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(request.FcmToken))
                    {
                        user.FcmToken = request.FcmToken;
                    }

                    var fullName = firebaseToken.Claims.TryGetValue("name", out var nameClaim)
                        ? nameClaim?.ToString()
                        : null;
                    var picture = firebaseToken.Claims.TryGetValue("picture", out var pictureClaim)
                        ? pictureClaim?.ToString()
                        : null;

                    if (!string.IsNullOrWhiteSpace(fullName) && string.IsNullOrWhiteSpace(user.FirstName) && string.IsNullOrWhiteSpace(user.LastName))
                    {
                        var nameParts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                        user.FirstName = nameParts.Length > 0 ? nameParts[0] : "Google";
                        user.LastName = nameParts.Length > 1 ? nameParts[1] : "User";
                    }

                    if (!string.IsNullOrWhiteSpace(picture) && string.IsNullOrWhiteSpace(user.AvatarUrl))
                    {
                        user.AvatarUrl = picture;
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(await GenerateAuthResponse(user));
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = "Token de Google inválido", detail = ex.Message });
            }
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

            var normalizedEmail = NormalizeEmail(request.Email);

            var emailExists = await _context.Users.AnyAsync(u =>
                u.Email == normalizedEmail && u.Id != user.Id
            );

            if (emailExists)
            {
                return BadRequest(new { message = "El email ya está registrado" });
            }

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.Email = normalizedEmail;
            user.Phone = request.Phone;

            await _context.SaveChangesAsync();

            return Ok(user.Adapt<UserResponse>());
        }

        [HttpPost("avatar")]
        [DisableRequestSizeLimit]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAvatar(IFormFile? avatar)
        {
            if (avatar == null || avatar.Length == 0)
                return BadRequest(new { message = "No se recibió ninguna imagen" });

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "Usuario no encontrado" });

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "avatars");
            Directory.CreateDirectory(uploadsFolder);

            if (!string.IsNullOrWhiteSpace(user.AvatarUrl))
            {
                var oldRelative = user.AvatarUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", oldRelative);
                if (System.IO.File.Exists(oldPath))
                {
                    System.IO.File.Delete(oldPath);
                }
            }

            var ext = Path.GetExtension(avatar.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            await using (var stream = System.IO.File.Create(filePath))
            {
                await avatar.CopyToAsync(stream);
            }

            user.AvatarUrl = $"/images/avatars/{fileName}";
            await _context.SaveChangesAsync();

            return Ok(user.Adapt<UserResponse>());
        }

        // POST: api/auth/refresh
        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return BadRequest(new { message = "Refresh token es requerido" });

            var tokenHash = _jwtTokenService.HashRefreshToken(request.RefreshToken);

            var storedToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

            if (storedToken == null || !storedToken.IsActive)
                return Unauthorized(new { message = "Refresh token inválido o expirado" });

            storedToken.RevokedAt = DateTime.UtcNow;

            var user = storedToken.User;
            var authResponse = await GenerateAuthResponse(user);

            return Ok(authResponse);
        }

        // POST: api/auth/logout
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // En JWT, el logout se maneja en el cliente eliminando el token
            return Ok(new { message = "Logout exitoso" });
        }

        [HttpGet("validate")]
        public async Task<IActionResult> ValidateJwt()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var user = await _context.Users
                .ProjectToType<UserResponse>()
                .FirstOrDefaultAsync(x => x.Id == userId);

            return Ok(user);
        }

        // PATCH: api/auth/password
        [ServiceFilter(typeof(UserExists))]
        [HttpPatch("password")]
        public async Task<IActionResult> UpdatePassword(UpdatePasswordRequest request)
        {
            var userIdValue = HttpContext.Items[UserIdKey];
            if (userIdValue is not int userId)
                return Unauthorized(new { message = "Usuario no autenticado" });

            var user = await _context.Users.FindAsync(userId);

            if (!_hashService.VerifyPassword(request.Old, user!.Password))
                return BadRequest(new { message = "La contraseña actual es incorrecta" });

            user.Password = _hashService.HashPassword(request.New);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Contraseña actualizada exitosamente" });
        }

        private async Task<AuthResponse> GenerateAuthResponse(User user)
        {
            var token = _jwtTokenService.GenerateToken(user);

            var rawRefreshToken = _jwtTokenService.GenerateRefreshToken();
            var refreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = _jwtTokenService.HashRefreshToken(rawRefreshToken),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            _context.RefreshTokens.Add(refreshTokenEntity);
            await _context.SaveChangesAsync();

            return new AuthResponse
            {
                Token = token,
                User = user.Adapt<UserResponse>(),
                RefreshToken = rawRefreshToken,
                RefreshTokenExpiresAt = refreshTokenEntity.ExpiresAt
            };
        }

        // PATCH: api/auth/fcm-token
        [HttpPatch("fcm-token")]
        public async Task<IActionResult> UpdateFcmToken([FromBody] FcmTokenRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "Usuario no encontrado" });

            user.FcmToken = request.Token;
            await _context.SaveChangesAsync();

            return Ok(user.Adapt<UserResponse>());
        }
    }
}
