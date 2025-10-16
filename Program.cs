using MongoDB.Driver;
using RealEstate.API.Services;
using Microsoft.Extensions.Caching.Memory;
using DotNetEnv;
using RealEstate.API.Middleware;
using FluentValidation;
using FluentValidation.AspNetCore;
using AutoMapper;
using RealEstate.API.Mappings;

// 🔹 Cargar variables de entorno antes de crear el builder
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// 🔹 Agregar variables de entorno al Configuration de .NET
builder.Configuration.AddEnvironmentVariables();

// === CONFIGURACIÓN DE SERVICIOS ===

// Controladores con FluentValidation y JSON legible
builder.Services.AddControllers()
    .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<Program>())
    .AddJsonOptions(options =>
    {
        // Evita escape de comillas y caracteres especiales
        options.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        options.JsonSerializerOptions.WriteIndented = true; // JSON bonito para debugging
    });

// Caché en memoria
builder.Services.AddMemoryCache();

// Servicio de Propiedades
builder.Services.AddSingleton<PropertyService>(sp =>
{
    var cache = sp.GetRequiredService<IMemoryCache>();
    return new PropertyService(builder.Configuration, cache);
});

// AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

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
