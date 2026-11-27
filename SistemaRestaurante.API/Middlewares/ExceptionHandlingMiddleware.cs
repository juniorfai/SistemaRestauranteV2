using Microsoft.AspNetCore.Mvc;
using SistemaRestaurante.Domain.Interfaces;

namespace SistemaRestaurante.API.Middlewares
{
    /// <summary>
    /// Convierte las excepciones de negocio en respuestas HTTP con formato ProblemDetails (RFC 7807):
    /// UnauthorizedAccessException → 401, ForbiddenAccessException → 403, KeyNotFoundException → 404, InvalidOperationException → 409, ArgumentException → 400, resto → 500.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger,
            IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex) when (!context.Response.HasStarted)
            {
                var (status, title) = ex switch
                {
                    UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "No autorizado"),
                    ForbiddenAccessException => (StatusCodes.Status403Forbidden, "Acceso denegado"),
                    KeyNotFoundException => (StatusCodes.Status404NotFound, "Recurso no encontrado"),
                    InvalidOperationException => (StatusCodes.Status409Conflict, "Operación no permitida"),
                    ArgumentException => (StatusCodes.Status400BadRequest, "Datos no válidos"),
                    _ => (StatusCodes.Status500InternalServerError, "Error interno del servidor")
                };

                if (status == StatusCodes.Status500InternalServerError)
                    _logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
                else
                    _logger.LogWarning("{Title}: {Message}", title, ex.Message);

                var problem = new ProblemDetails
                {
                    Status = status,
                    Title = title,
                    // Los mensajes de negocio son seguros de mostrar; los errores internos solo en desarrollo.
                    Detail = status != StatusCodes.Status500InternalServerError || _environment.IsDevelopment()
                        ? ex.Message
                        : "Ocurrió un error inesperado. Inténtalo de nuevo más tarde.",
                    Instance = context.Request.Path
                };

                context.Response.StatusCode = status;
                await context.Response.WriteAsJsonAsync(problem, (System.Text.Json.JsonSerializerOptions?)null,
                    "application/problem+json");
            }
        }
    }
}
