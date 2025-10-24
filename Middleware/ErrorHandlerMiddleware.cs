using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using RealEstate.API.Infraestructure.Core.Logs; // Donde está tu ServiceLogResponseWrapper

namespace RealEstate.API.Middleware
{
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
            // 🔹 Determinar tipo de error → status code adecuado
            HttpStatusCode statusCode = ex switch
            {
                UnauthorizedAccessException => HttpStatusCode.Unauthorized,
                KeyNotFoundException => HttpStatusCode.NotFound,
                InvalidOperationException => HttpStatusCode.Conflict,
                ArgumentException => HttpStatusCode.BadRequest,
                _ => HttpStatusCode.InternalServerError
            };

            // 🔹 Registrar error en logs
            _logger.LogError(ex, "❌ Error capturado en middleware global: {Message}", ex.Message);

            // 🔹 Armar respuesta unificada
            var response = ServiceLogResponseWrapper<string>.Fail(
                message: "Ocurrió un error al procesar la solicitud",
                errors: new[] { ex.Message },
                statusCode: (int)statusCode
            );

            // 🔹 Configurar headers y serializar JSON
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(response, jsonOptions);
            await context.Response.WriteAsync(json);
        }
    }
}
