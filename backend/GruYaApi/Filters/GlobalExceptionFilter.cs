using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ConAlambreApi.Filters
{
    public class GlobalExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<GlobalExceptionFilter> _logger;

        public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            var e = context.Exception;
            _logger.LogError(e, e.Message);

            ProblemDetails respuesta;

            if (
                e is DbUpdateException dbE
                && dbE.InnerException is NpgsqlException npgsqlE
                && npgsqlE.SqlState == "23505"
            )
            {
                respuesta = new ProblemDetails
                {
                    Title = "Entrada duplicada",
                    Detail = "Ya existe una entidad con esos datos",
                    Status = 500,
                    Type = "",
                };
                context.Result = new ObjectResult(respuesta) { StatusCode = respuesta.Status };
                context.ExceptionHandled = true;
                return;
            }

            if (e is DbUpdateException)
            {
                respuesta = new ProblemDetails
                {
                    Title = "Error en la base de datos",
                    Detail = "Ocurrió un error inesperado en la base de datos",
                    Status = 500,
                    Type = "",
                };
                context.Result = new ObjectResult(respuesta) { StatusCode = respuesta.Status };
                context.ExceptionHandled = true;
                return;
            }

            respuesta = new ProblemDetails
            {
                Title = "Error en el servidor",
                Detail = "Ocurrió un error inesperado en el servidor",
                Status = 500,
                Type = "",
            };
            context.Result = new ObjectResult(respuesta) { StatusCode = respuesta.Status };
            context.ExceptionHandled = true;
        }
    }
}
