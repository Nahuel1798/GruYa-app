using System.Security.Claims;
using GruYaApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace GruYaApi.Filters
{
    public class UserExists : IAsyncActionFilter
    {
        private readonly DataContext _context;
        private readonly ILogger<UserExists> _logger;

        public UserExists(DataContext context, ILogger<UserExists> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next
        )
        {
            var idUser = int.Parse(
                context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0"
            );

            if (idUser == 0)
            {
                context.Result = new UnauthorizedObjectResult("Error de autenticación");
                return;
            }

            var path = context.HttpContext.Request.Path;
            var method = context.HttpContext.Request.Method;
            _logger.LogInformation("UserExists filter: {Method} {Path} for userId={UserId}", method, path, idUser);

            var user = await _context.Users.AnyAsync(u => u.Id == idUser);

            if (!user)
            {
                context.Result = new UnauthorizedObjectResult("Error de autenticación");
                return;
            }

            context.HttpContext.Items.Add("idUsuario", idUser);

            await next();
        }
    }
}
