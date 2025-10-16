using MongoDB.Driver;
using RealEstate.API.Services;
using Microsoft.Extensions.Caching.Memory;
using DotNetEnv;
using RealEstate.API.Middleware;
using FluentValidation.AspNetCore;
using AutoMapper; // <- Asegúrate de importar AutoMapper
using RealEstate.API.Mappings; // <- Importa tu MappingProfile

// 🔹 Cargar archivo .env antes de crear el builder
DotNetEnv.Env.Load(); 

var builder = WebApplication.CreateBuilder(args);

// 🔹 Agregar variables de entorno al Configuration de .NET
builder.Configuration.AddEnvironmentVariables();

// === CONFIGURACIÓN DE SERVICIOS ===

// Controladores con FluentValidation
builder.Services.AddControllers()
    .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<Program>());

// Caché en memoria
builder.Services.AddMemoryCache();

// Servicio de Propiedades
builder.Services.AddSingleton<PropertyService>(sp =>
{
    var cache = sp.GetRequiredService<IMemoryCache>();
    return new PropertyService(builder.Configuration, cache);
});

// AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile)); // <- Línea agregada

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

// Middleware global de errores (centralizado)
app.UseMiddleware<ErrorHandlerMiddleware>();

// Mapear controladores
app.MapControllers();

app.Run();
