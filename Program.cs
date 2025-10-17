using MongoDB.Driver;
using RealEstate.API.Services;
using Microsoft.Extensions.Caching.Memory;
using DotNetEnv;
using RealEstate.API.Middleware;
using FluentValidation;
using FluentValidation.AspNetCore;
using AutoMapper;
using RealEstate.API.Mappings;
using Microsoft.AspNetCore.Mvc.NewtonsoftJson; // 👈 necesario para AddNewtonsoftJson()

// ==========================================
// 🔹 CARGA DE VARIABLES DE ENTORNO
// ==========================================
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Agregar variables de entorno al Configuration de .NET
builder.Configuration.AddEnvironmentVariables();

// ==========================================
// 🔹 CONFIGURACIÓN DE SERVICIOS
// ==========================================

// Controladores con soporte completo para PATCH y JSON flexible
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        // 👇 Configuración de Newtonsoft para que soporte JSON Patch y evite conflictos de etiquetas por mayúsculas y minúsculas
        options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        options.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
    })
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
builder.Services.AddAutoMapper(typeof(MappingProfile));

// CORS global (permitir todo en desarrollo)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// ==========================================
// 🔹 CONSTRUCCIÓN DE LA APP
// ==========================================
var app = builder.Build();

// ==========================================
// 🔹 MIDDLEWARE Y PIPELINE
// ==========================================
app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Middleware global de errores
app.UseMiddleware<ErrorHandlerMiddleware>();

// Mapear controladores
app.MapControllers();

// ==========================================
// 🔹 EJECUCIÓN
// ==========================================
app.Run();
