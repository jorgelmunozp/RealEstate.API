using MongoDB.Driver;
using RealEstate.API.Services;
using Microsoft.Extensions.Caching.Memory;
using DotNetEnv;
using Microsoft.OpenApi.Models;
using System.Reflection;

// 🔹 Cargar archivo .env antes de crear el builder
DotNetEnv.Env.Load(); 

var builder = WebApplication.CreateBuilder(args);

// 🔹 Agregar variables de entorno al Configuration de .NET
builder.Configuration.AddEnvironmentVariables();

// === CONFIGURACIÓN DE SERVICIOS ===

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RealEstate API",
        Version = "v1",
        Description = "API para gestionar propiedades inmobiliarias"
    });

    // Configuración para leer comentarios XML
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

// Controladores
builder.Services.AddControllers();

// Caché en memoria
builder.Services.AddMemoryCache();

// Servicio de Propiedades, pasando IConfiguration y IMemoryCache
builder.Services.AddSingleton<PropertyService>(sp =>
{
    var cache = sp.GetRequiredService<IMemoryCache>();
    return new PropertyService(builder.Configuration, cache);
});

// === CORS (para permitir peticiones desde el frontend) ===
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",   // React
                "https://localhost:3000",  // React HTTPS
                "http://localhost:5173",   // Vite
                "https://localhost:5173"   // Vite HTTPS
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// === PIPELINE ===
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "RealEstate API V1");
    c.RoutePrefix = string.Empty; // Swagger en la raíz
});


app.UseHttpsRedirection();

// Activar CORS
app.UseCors("AllowFrontend");

// === MIDDLEWARE GLOBAL DE ERRORES ===
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (MongoConnectionException ex)
    {
        context.Response.StatusCode = 503;
        await context.Response.WriteAsJsonAsync(new { error = "No se pudo conectar a la base de datos MongoDB", detail = ex.Message });
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

// === ENDPOINTS CONTROLADOS ===
app.MapControllers();

app.Run();
