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

        public UserExists(DataContext context)
        {
            _context = context;
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
                context.Result = new UnauthorizedObjectResult("Error de autenticación");

            var user = await _context.Users.AnyAsync(u => u.Id == idUser);

            if (!user)
                context.Result = new UnauthorizedObjectResult("Error de autenticación");

            context.HttpContext.Items.Add("idUsuario", idUser);

            await next();
        }
    }
}
