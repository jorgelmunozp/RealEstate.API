using System.Text;
using DotNetEnv;
using MongoDB.Driver;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;

using RealEstate.API.Middleware;
using RealEstate.API.Mappings;

// Auth
using RealEstate.API.Modules.Auth.Dto;
using RealEstate.API.Modules.Auth.Validator;
using RealEstate.API.Modules.Auth.Service;

// User
using RealEstate.API.Modules.User.Dto;
using RealEstate.API.Modules.User.Validator;
using RealEstate.API.Modules.User.Service;

// Property
using RealEstate.API.Modules.Property.Dto;
using RealEstate.API.Modules.Property.Validator;
using RealEstate.API.Modules.Property.Service;

// Owner
using RealEstate.API.Modules.Owner.Dto;
using RealEstate.API.Modules.Owner.Validator;
using RealEstate.API.Modules.Owner.Service;

// PropertyImage
using RealEstate.API.Modules.PropertyImage.Dto;
using RealEstate.API.Modules.PropertyImage.Validator;
using RealEstate.API.Modules.PropertyImage.Service;

// PropertyTrace
using RealEstate.API.Modules.PropertyTrace.Dto;
using RealEstate.API.Modules.PropertyTrace.Validator;
using RealEstate.API.Modules.PropertyTrace.Service;

DotNetEnv.Env.Load();
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
var config = builder.Configuration;

// ==========================================
// 🔹 CONFIGURACIÓN DE MONGODB
// ==========================================
var mongoConnectionString = config["MONGO_CONNECTION"] ?? "mongodb://localhost:27017";
var mongoDbName = config["MONGO_DATABASE"] ?? "RealEstate";

if (string.IsNullOrWhiteSpace(mongoConnectionString))
    throw new InvalidOperationException("La variable de entorno MONGO_CONNECTION no puede ser nula o vacía.");
if (string.IsNullOrWhiteSpace(mongoDbName))
    throw new InvalidOperationException("La variable de entorno MONGO_DATABASE no puede ser nula o vacía.");

Console.WriteLine($"MongoDB Connection: {mongoConnectionString}");
Console.WriteLine($"MongoDB Database: {mongoDbName}");

builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnectionString));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(mongoDbName));

// ==========================================
// 🔹 JSON SIN camelCase
// ==========================================
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = null;
        o.JsonSerializerOptions.DictionaryKeyPolicy = null;
    });

// ==========================================
// 🔹 JWT DESDE VARIABLES DE ENTORNO
// ==========================================
var secretKey = config["JWT_SECRET"] ?? throw new InvalidOperationException("La variable JWT_SECRET no está definida");
var issuer = config["JWT_ISSUER"] ?? "RealEstateAPI";
var audience = config["JWT_AUDIENCE"] ?? "UsuariosAPI";
var expiryMinutes = config["JWT_EXPIRY"] ?? "60";

builder.Configuration["JwtSettings:SecretKey"] = secretKey;
builder.Configuration["JwtSettings:Issuer"] = issuer;
builder.Configuration["JwtSettings:Audience"] = audience;
builder.Configuration["JwtSettings:ExpiryMinutes"] = expiryMinutes;

// ==========================================
// 🔹 VALIDACIÓN GLOBAL Y FLUENTVALIDATION
// ==========================================
builder.Services.AddControllers(o => o.Filters.Add<ValidationExceptionFilter>())
.AddNewtonsoftJson(o =>
{
    o.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
    o.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
    o.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
});

builder.Services.AddFluentValidationAutoValidation()
                .AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.Configure<ApiBehaviorOptions>(o =>
{
    o.InvalidModelStateResponseFactory = ctx =>
    {
        var errors = ctx.ModelState
            .Where(x => x.Value.Errors.Count > 0)
            .ToDictionary(k => k.Key, v => v.Value.Errors.Select(e => e.ErrorMessage).ToArray());

        return new BadRequestObjectResult(new { message = "Errores de validación", errors });
    };
});

// ==========================================
// 🔹 CACHÉ
// ==========================================
builder.Services.AddMemoryCache();

// ==========================================
// 🔹 SERVICIOS
// ==========================================
builder.Services.AddScoped<IValidator<LoginDto>, LoginDtoValidator>();
builder.Services.AddScoped<AuthService>();

builder.Services.AddScoped<IValidator<UserDto>, UserDtoValidator>();
builder.Services.AddScoped<UserService>();

builder.Services.AddScoped<IValidator<PropertyDto>, PropertyDtoValidator>();
builder.Services.AddScoped<PropertyService>();

builder.Services.AddScoped<IValidator<OwnerDto>, OwnerDtoValidator>();
builder.Services.AddScoped<OwnerService>();

builder.Services.AddScoped<IValidator<PropertyImageDto>, PropertyImageDtoValidator>();
builder.Services.AddScoped<PropertyImageService>();

builder.Services.AddScoped<IValidator<PropertyTraceDto>, PropertyTraceDtoValidator>();
builder.Services.AddScoped<PropertyTraceService>();

builder.Services.AddSingleton<JwtService>();

// ==========================================
// 🔹 CORS
// ==========================================
builder.Services.AddCors(o =>
{
    o.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// ==========================================
// 🔹 JWT AUTENTICACIÓN
// ==========================================
var keyBytes = Encoding.UTF8.GetBytes(secretKey);
builder.Services.AddAuthentication(o =>
{
    o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
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

// ==========================================
// 🔹 LOGGING
// ==========================================
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(o =>
{
    o.TimestampFormat = "[HH:mm:ss] ";
    o.SingleLine = true;
});

// ==========================================
// 🔹 APP
// ==========================================
var app = builder.Build();
app.UseMiddleware<LoggingMiddleware>();
app.UseMiddleware<ErrorHandlerMiddleware>();

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
