using MongoDB.Driver;
using RealEstate.API.Models;
using RealEstate.API.Services;

var builder = WebApplication.CreateBuilder(args);

// === CONFIGURACIÓN DE SERVICIOS ===

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Servicio de Propiedades (inyectado como singleton)
builder.Services.AddSingleton<PropertyService>();

var app = builder.Build();

// === PIPELINE ===
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// === MIDDLEWARE GLOBAL DE ERRORES ===
// Toda excepción no controlada sea atrapada y devuelva un JSON claro al cliente (y no una página de error .NET)
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


// === ENDPOINTS ===

// Obtener todas las propiedades con filtros
app.MapGet("/api/property", async (
    string? name,
    string? address,
    decimal? minPrice,
    decimal? maxPrice,
    PropertyService service) =>
{
    var properties = await service.GetAllAsync(name, address, minPrice, maxPrice);
    return Results.Ok(properties);
})
.WithName("GetAllProperties")
.WithOpenApi();

// Obtener propiedad por ID
app.MapGet("/api/property/{id}", async (string id, PropertyService service) =>
{
    var property = await service.GetByIdAsync(id);
    return property is not null ? Results.Ok(property) : Results.NotFound();
})
.WithName("GetPropertyById")
.WithOpenApi();

app.Run();
