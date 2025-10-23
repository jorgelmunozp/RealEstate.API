using System.Diagnostics;
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

        // ----- Antes de procesar -----
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("[HALL] ");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("Request: ");
        Console.ResetColor();
        Console.WriteLine($"[{method}] {url}");

        _logger.LogInformation("HALL Request: [{Method}] {Url}", method, url);

        await _next(context);

        stopwatch.Stop();
        var statusCode = context.Response.StatusCode;
        var responseTime = stopwatch.ElapsedMilliseconds;

        // ----- Después de procesar -----
        Console.ForegroundColor = statusCode >= 400 ? ConsoleColor.Red : ConsoleColor.Yellow;
        Console.Write("[HALL] ");
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.Write("Response: ");
        Console.ResetColor();
        Console.WriteLine($"[{method}] {url} => {statusCode} ({responseTime}ms)");

        _logger.LogInformation(
            "HALL Response: [{Method}] {Url} => {StatusCode} ({ResponseTime}ms)",
            method,
            url,
            statusCode,
            responseTime
        );
    }
}
