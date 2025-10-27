using System.Text;
using DotNetEnv;
using MongoDB.Driver;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;

// Core
using RealEstate.API.Middleware;
using RealEstate.API.Mappings;
using RealEstate.API.Infraestructure.Core.Services;

// Token / Auth / Password
using RealEstate.API.Modules.Token.Service;
using RealEstate.API.Modules.Auth.Dto;
using RealEstate.API.Modules.Auth.Validator;
using RealEstate.API.Modules.Auth.Service;
using RealEstate.API.Modules.Password.Service;

// User
using RealEstate.API.Modules.User.Dto;
using RealEstate.API.Modules.User.Validator;
using RealEstate.API.Modules.User.Service;

// Property / Owner / Image / Trace
using RealEstate.API.Modules.Property.Dto;
using RealEstate.API.Modules.Property.Validator;
using RealEstate.API.Modules.Property.Service;
using RealEstate.API.Modules.Owner.Dto;
using RealEstate.API.Modules.Owner.Validator;
using RealEstate.API.Modules.Owner.Service;
using RealEstate.API.Modules.PropertyImage.Dto;
using RealEstate.API.Modules.PropertyImage.Validator;
using RealEstate.API.Modules.PropertyImage.Service;
using RealEstate.API.Modules.PropertyTrace.Dto;
using RealEstate.API.Modules.PropertyTrace.Validator;
using RealEstate.API.Modules.PropertyTrace.Service;

// ===========================================================
// 🔹 CONFIGURACIÓN BASE
// ===========================================================
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
var config = builder.Configuration;

// ===========================================================
// 🔹 MONGODB
// ===========================================================
var mongoConnection = config["MONGO_CONNECTION"] ?? "mongodb://localhost:27017";
var mongoDatabase = config["MONGO_DATABASE"] ?? "RealEstate";

if (string.IsNullOrWhiteSpace(mongoConnection))
    throw new InvalidOperationException("La variable MONGO_CONNECTION no puede ser nula o vacía.");
if (string.IsNullOrWhiteSpace(mongoDatabase))
    throw new InvalidOperationException("La variable MONGO_DATABASE no puede ser nula o vacía.");

Console.WriteLine($"[MongoDB] Conexión: {mongoConnection}");
Console.WriteLine($"[MongoDB] Base de datos: {mongoDatabase}");

builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnection));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(mongoDatabase));

// ===========================================================
// 🔹 JSON GLOBAL (camelCase + case-insensitive)
// ===========================================================
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// ===========================================================
// 🔹 JWT CONFIG (desde entorno)
// ===========================================================
var secretKey = config["JWT_SECRET"] ?? throw new InvalidOperationException("JWT_SECRET no está definida");
var issuer = config["JWT_ISSUER"] ?? "RealEstateAPI";
var audience = config["JWT_AUDIENCE"] ?? "UsuariosAPI";
var expiryMinutes = config["JWT_EXPIRY_MINUTES"] ?? "60";
var refreshDays = config["JWT_REFRESH_DAYS"] ?? "7";

builder.Configuration["JwtSettings:SecretKey"] = secretKey;
builder.Configuration["JwtSettings:Issuer"] = issuer;
builder.Configuration["JwtSettings:Audience"] = audience;
builder.Configuration["JwtSettings:ExpiryMinutes"] = expiryMinutes;
builder.Configuration["JwtSettings:RefreshDays"] = refreshDays;

// ===========================================================
// 🔹 VALIDACIÓN GLOBAL (FluentValidation + Wrapper uniforme)
// ===========================================================
builder.Services.AddFluentValidationAutoValidation()
                .AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<Program>(ServiceLifetime.Scoped);

// Nombres de propiedades camelCase en validaciones
FluentValidation.ValidatorOptions.Global.PropertyNameResolver = (type, member, expr) =>
{
    var name = member?.Name ?? expr?.ToString()?.Split('.').LastOrDefault();
    return string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];
};

// Respuesta uniforme para errores de validación
builder.Services.Configure<ApiBehaviorOptions>(opt =>
{
    opt.InvalidModelStateResponseFactory = ctx =>
    {
        var errors = ctx.ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .SelectMany(v => v.Value!.Errors)
            .Select(e => e.ErrorMessage)
            .ToArray();

        var payload = ServiceResultWrapper<object>.Fail(
            errors,
            statusCode: 400,
            message: "Errores de validación"
        );

        return new BadRequestObjectResult(payload);
    };
});

// ===========================================================
// 🔹 CACHÉ EN MEMORIA
// ===========================================================
builder.Services.AddMemoryCache();

// ===========================================================
// 🔹 INYECCIÓN DE DEPENDENCIAS (Servicios / Validadores)
// ===========================================================

// Auth / Token / Password
builder.Services.AddScoped<IValidator<LoginDto>, LoginDtoValidator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<PasswordService>();

// User
builder.Services.AddScoped<IValidator<UserDto>, UserDtoValidator>();
builder.Services.AddScoped<UserService>();

// Owner
builder.Services.AddScoped<IValidator<OwnerDto>, OwnerDtoValidator>();
builder.Services.AddScoped<OwnerService>();

// PropertyImage y Trace (antes de PropertyService)
builder.Services.AddScoped<IValidator<PropertyImageDto>, PropertyImageDtoValidator>();
builder.Services.AddScoped<PropertyImageService>();
builder.Services.AddScoped<IValidator<PropertyTraceDto>, PropertyTraceDtoValidator>();
builder.Services.AddScoped<PropertyTraceService>();

// Property principal
builder.Services.AddScoped<IValidator<PropertyDto>, PropertyDtoValidator>();
builder.Services.AddScoped<PropertyService>();

// ===========================================================
// 🔹 AUTOMAPPER (perfil de mapeos globales)
// ===========================================================
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);

// ===========================================================
// 🔹 CORS
// ===========================================================
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// ===========================================================
// 🔹 AUTENTICACIÓN JWT
// ===========================================================
var keyBytes = Encoding.UTF8.GetBytes(secretKey);
builder.Services.AddAuthentication(opt =>
{
    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(opt =>
{
    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// ===========================================================
// 🔹 LOGGING
// ===========================================================
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(o =>
{
    o.TimestampFormat = "[HH:mm:ss] ";
    o.SingleLine = true;
});

// ===========================================================
// 🔹 PIPELINE DE MIDDLEWARE
// ===========================================================
var app = builder.Build();

// Orden recomendado
app.UseMiddleware<LoggingMiddleware>();
app.UseMiddleware<ErrorHandlerMiddleware>();

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// ===========================================================
// 🔹 STATUS CODE PAGES (respuestas JSON unificadas)
// ===========================================================
app.UseStatusCodePages(async context =>
{
    var res = context.HttpContext.Response;
    var msg = res.StatusCode switch
    {
        401 => "No autorizado",
        403 => "Prohibido",
        404 => "Recurso no encontrado",
        405 => "Método no permitido",
        415 => "Tipo de contenido no soportado",
        _ => "Error inesperado"
    };

    var payload = ServiceResultWrapper<string>.Fail(msg, res.StatusCode);
    res.ContentType = "application/json";

    var json = System.Text.Json.JsonSerializer.Serialize(
        payload,
        new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        }
    );

    await res.WriteAsync(json);
});

app.MapControllers();
app.Run();
