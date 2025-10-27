using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

public class LoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        var request = context.Request;
        var method = request.Method;
        var url = request.Path;

        // Detectar Id de la propiedad o id genérico
        var propertyId = context.Request.RouteValues.TryGetValue("propertyId", out var pId)
            ? pId?.ToString()
            : context.Request.RouteValues.TryGetValue("id", out var id)
                ? id?.ToString()
                : null;

        // Leer cuerpo del request (solo POST, PUT, PATCH)
        string requestBody = string.Empty;
        if (method is "POST" or "PUT" or "PATCH")
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            // Evitar saturación si el body contiene imágenes base64
            if (requestBody.Length > 1500)
                requestBody = requestBody.Substring(0, 1500) + "... [truncated]";
        }

        // Capturar cuerpo de la respuesta
        var originalBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        // ======== LOG REQUEST ========
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("[HALL] ");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("Request: ");
        Console.ResetColor();
        Console.WriteLine($"[{method}] {url} (PropertyId: {propertyId ?? "N/A"})");

        if (!string.IsNullOrWhiteSpace(requestBody))
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"Body: {requestBody}");
            Console.ResetColor();
        }

        _logger.LogInformation(
            "HALL Request: [{Method}] {Url} (PropertyId: {PropertyId}) Body: {Body}",
            method, url, propertyId, string.IsNullOrWhiteSpace(requestBody) ? "No body" : requestBody
        );

        await _next(context); // Ejecuta el resto del pipeline

        stopwatch.Stop();

        // Leer respuesta
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        context.Response.Body.Seek(0, SeekOrigin.Begin);

        // Restaurar stream original
        await responseBodyStream.CopyToAsync(originalBodyStream);
        context.Response.Body = originalBodyStream;

        var statusCode = context.Response.StatusCode;
        var responseTime = stopwatch.ElapsedMilliseconds;

        // ======== LOG RESPONSE ========
        Console.ForegroundColor = statusCode >= 400 ? ConsoleColor.Red : ConsoleColor.Yellow;
        Console.Write("[HALL] ");
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.Write("Response: ");
        Console.ResetColor();
        Console.WriteLine($"[{method}] {url} => {statusCode} ({responseTime}ms) (PropertyId: {propertyId ?? "N/A"})");

        if (!string.IsNullOrWhiteSpace(responseBody))
        {
            if (responseBody.Length > 1500)
                responseBody = responseBody.Substring(0, 1500) + "... [truncated]";

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"Response Body: {responseBody}");
            Console.ResetColor();
        }

        _logger.LogInformation(
            "HALL Response: [{Method}] {Url} => {StatusCode} ({ResponseTime}ms) (PropertyId: {PropertyId}) Body: {Body}",
            method,
            url,
            statusCode,
            responseTime,
            propertyId,
            string.IsNullOrWhiteSpace(responseBody) ? "No body" : responseBody
        );

        // Log especial para PATCH (detección rápida)
        if (method == "PATCH")
            Console.WriteLine($"[LOG PATCH] Campos actualizados detectados en {url}");
    }
}
