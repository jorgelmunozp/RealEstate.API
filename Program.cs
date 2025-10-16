using MongoDB.Driver;
using RealEstate.API.Services;
using Microsoft.Extensions.Caching.Memory;
using DotNetEnv;

// 🔹 Cargar archivo .env antes de crear el builder
DotNetEnv.Env.Load(); 

var builder = WebApplication.CreateBuilder(args);

// 🔹 Agregar variables de entorno al Configuration de .NET
builder.Configuration.AddEnvironmentVariables();

// === CONFIGURACIÓN DE SERVICIOS ===

// Controladores
builder.Services.AddControllers();

// Caché en memoria
builder.Services.AddMemoryCache();

// Servicio de Propiedades
builder.Services.AddSingleton<PropertyService>(sp =>
{
    var cache = sp.GetRequiredService<IMemoryCache>();
    return new PropertyService(builder.Configuration, cache);
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",
                "https://localhost:3000",
                "http://localhost:5173",
                "https://localhost:5173"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// === PIPELINE ===

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

// Middleware global de errores
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (MongoConnectionException ex)
    {
        context.Response.StatusCode = 503;
        await context.Response.WriteAsJsonAsync(new { error = "No se pudo conectar a MongoDB", detail = ex.Message });
    }
    catch (FormatException ex)
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsJsonAsync(new { error = "Formato de datos inválido", detail = ex.Message });
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new { error = "Error interno del servidor", detail = ex.Message });
    }
});

// Mapear controladores
app.MapControllers();

app.Run();
