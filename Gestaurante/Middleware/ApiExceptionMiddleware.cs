using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Gestaurante.Middleware
{
    public class ApiExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiExceptionMiddleware> _logger;

        public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                _logger.LogWarning("La petición {Method} {Path} fue cancelada por el cliente.", context.Request.Method, context.Request.Path);
            }
            catch (Exception exception)
            {
                await HandleExceptionAsync(context, exception);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (status, message, level) = exception switch
            {
                KeyNotFoundException => (StatusCodes.Status404NotFound, exception.Message, LogLevel.Information),
                UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, exception.Message, LogLevel.Warning),
                ValidationException => (StatusCodes.Status422UnprocessableEntity, exception.Message, LogLevel.Information),
                ArgumentException => (StatusCodes.Status400BadRequest, exception.Message, LogLevel.Information),
                InvalidOperationException => (StatusCodes.Status409Conflict, exception.Message, LogLevel.Information),
                _ => (StatusCodes.Status500InternalServerError, "Ha ocurrido un error interno en el servidor.", LogLevel.Error)
            };

            _logger.Log(level, exception, "Error controlado en {Method} {Path}. Status={Status}", context.Request.Method, context.Request.Path, status);

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json";

            var payload = JsonSerializer.Serialize(new
            {
                status,
                error = message
            });

            await context.Response.WriteAsync(payload);
        }
    }
}
