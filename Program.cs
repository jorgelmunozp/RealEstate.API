using MongoDB.Driver;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using DotNetEnv;
using FluentValidation;
using FluentValidation.AspNetCore;
using RealEstate.API.Middleware;
using RealEstate.API.Modules.Auth.Dto;
using RealEstate.API.Modules.Auth.Validator;

// Auth Service
using RealEstate.API.Modules.Auth.Service;

// Properties Service
using RealEstate.API.Modules.Properties.Service;

// User Service
using RealEstate.API.Modules.User.Dto;
using RealEstate.API.Modules.User.Validator;
using RealEstate.API.Modules.User.Service;

// Property Service
using RealEstate.API.Modules.Property.Dto;
using RealEstate.API.Modules.Property.Model;
using RealEstate.API.Modules.Property.Validator;
using RealEstate.API.Modules.Property.Service;

// Owner Service
using RealEstate.API.Modules.Owner.Dto;
using RealEstate.API.Modules.Owner.Validator;
using RealEstate.API.Modules.Owner.Service;

// PropertyTrace Service
using RealEstate.API.Modules.PropertyImage.Dto;
using RealEstate.API.Modules.PropertyImage.Validator;
using RealEstate.API.Modules.PropertyImage.Service;

// PropertyTrace Service
using RealEstate.API.Modules.PropertyTrace.Dto;
using RealEstate.API.Modules.PropertyTrace.Validator;
using RealEstate.API.Modules.PropertyTrace.Service;

// Mappings
using RealEstate.API.Mappings;



DotNetEnv.Env.Load();
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

// ==========================================
// CONFIGURACIÓN DE MONGODB
// ==========================================
var mongoConnectionString = Environment.GetEnvironmentVariable("MONGO_CONNECTION") 
                            ?? "mongodb://localhost:27017";
var mongoDbName = Environment.GetEnvironmentVariable("MONGO_DATABASE") 
                  ?? "RealEstate";

if (string.IsNullOrWhiteSpace(mongoConnectionString))
    throw new InvalidOperationException("La variable de entorno MONGO_CONNECTION no puede ser nula o vacía.");

if (string.IsNullOrWhiteSpace(mongoDbName))
    throw new InvalidOperationException("La variable de entorno MONGO_DATABASE no puede ser nula o vacía.");

Console.WriteLine($"MongoDB Connection: {mongoConnectionString}");
Console.WriteLine($"MongoDB Database: {mongoDbName}");

builder.Services.AddSingleton<IMongoClient>(sp => new MongoClient(mongoConnectionString));
builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase(mongoDbName);
});



// Deshabilita la conversión a camelCase
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.DictionaryKeyPolicy = null;
    });
// ==========================================
// CONFIGURACIÓN JWT DESDE VARIABLES DE ENTORNO
// ==========================================
var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET") 
                ?? throw new InvalidOperationException("La variable JWT_SECRET no está definida");
var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "RealEstateAPI";
var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "UsuariosAPI";
var expiryMinutes = Environment.GetEnvironmentVariable("JWT_EXPIRY") ?? "60";

// Mapear a IConfiguration para que JwtService funcione igual
builder.Configuration["JwtSettings:SecretKey"] = secretKey;
builder.Configuration["JwtSettings:Issuer"] = issuer;
builder.Configuration["JwtSettings:Audience"] = audience;
builder.Configuration["JwtSettings:ExpiryMinutes"] = expiryMinutes;

// ==========================================
// CONTROLADORES, VALIDACIONES Y FILTRO GLOBAL
// ==========================================
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationExceptionFilter>();
})
.AddNewtonsoftJson(options =>
{
    options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
    options.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
});

builder.Services.AddFluentValidationAutoValidation()
                .AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(x => x.Value.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
            );

        return new BadRequestObjectResult(new
        {
            message = "Errores de validación",
            errors
        });
    };
});

// ==========================================
// CACHÉ EN MEMORIA
// ==========================================
builder.Services.AddMemoryCache();

// ==========================================
// MICROSERVICIOS
// ==========================================

// Auth
builder.Services.AddScoped<IValidator<LoginDto>, LoginDtoValidator>();
builder.Services.AddScoped<AuthService>();

// User
builder.Services.AddScoped<IValidator<UserDto>, UserDtoValidator>();
builder.Services.AddScoped<UserService>();

// Property
builder.Services.AddScoped<IValidator<PropertyModel>, PropertyModelValidator>();
builder.Services.AddScoped<IValidator<PropertyDto>, PropertyDtoValidator>();
builder.Services.AddScoped<PropertyService>();

// Owner
builder.Services.AddScoped<IValidator<OwnerDto>, OwnerDtoValidator>();
builder.Services.AddScoped<OwnerService>();

// PropertyImage
builder.Services.AddScoped<IValidator<PropertyImageDto>, PropertyImageDtoValidator>();
builder.Services.AddScoped<PropertyImageService>();

// PropertyTrace
builder.Services.AddScoped<IValidator<PropertyTraceDto>, PropertyTraceDtoValidator>();
builder.Services.AddScoped<PropertyTraceService>();

// JWT Service
builder.Services.AddSingleton<JwtService>();
// ==========================================
// CORS GLOBAL
// ==========================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ==========================================
// CONFIGURACIÓN JWT PARA AUTENTICACIÓN
// ==========================================
var keyBytes = Encoding.UTF8.GetBytes(secretKey);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
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


// Limpiar providers y añadir consola para los logs con formato HALL
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
// ==========================================
// CONSTRUCCIÓN DE LA APP
// ==========================================
var app = builder.Build();

// ==========================================
// MIDDLEWARE
// ==========================================
// Middleware de logging HALL - Backend Logs
app.UseMiddleware<LoggingMiddleware>();

// Middleware de errores HTTP - Frontend Logs
app.UseMiddleware<ErrorHandlerMiddleware>();

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// ==========================================
// CONTROLADORES
// ==========================================
app.MapControllers();

// ==========================================
// EJECUCIÓN
// ==========================================
app.Run();
