using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using RealEstate.API.Infraestructure.Core.Services;

namespace RealEstate.API.Middleware
{
    
    // Middleware global de manejo de errores: Captura excepciones no controladas, las registra en consola y logs estructurados, y devuelve una respuesta JSON unificada con el formato del ServiceResultWrapper.
    public class ErrorHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlerMiddleware> _logger;

        public ErrorHandlerMiddleware(RequestDelegate next, ILogger<ErrorHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            // Determina el tipo de excepción → código HTTP adecuado
            var StatusCode = ex switch
            {
                UnauthorizedAccessException => HttpStatusCode.Unauthorized,
                KeyNotFoundException => HttpStatusCode.NotFound,
                InvalidOperationException => HttpStatusCode.Conflict,
                ArgumentException => HttpStatusCode.BadRequest,
                _ => HttpStatusCode.InternalServerError
            };

            // Consola: formato [HALL] consistente con LoggingMiddleware
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("[HALL] ");
            Console.ResetColor();
            Console.WriteLine($"StatusCode Error global capturado: {ex.GetType().Name}");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"Mensaje: {ex.Message}");
            Console.WriteLine($"Ruta: {context.Request.Method} {context.Request.Path}");
            if (ex.StackTrace != null)
                Console.WriteLine($"StackTrace:\n{ex.StackTrace}");
            Console.ResetColor();

            // Log estructurado con ILogger
            _logger.LogError(ex, "StatusCode Error global: {Message}", ex.Message);

            // Construir respuesta JSON uniforme
            var response = ServiceResultWrapper<string>.Error(ex, (int)StatusCode);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)StatusCode;

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(response, jsonOptions);
            await context.Response.WriteAsync(json);

            // Log resumen final del pipeline
            _logger.LogInformation(
                "[HALL] ErrorHandler => {StatusCode} {Method} {Path} ({ExceptionType})",
                (int)StatusCode,
                context.Request.Method,
                context.Request.Path,
                ex.GetType().Name
            );
        }
    }
}
