using MongoDB.Driver;
using RealEstate.API.Services;
using Microsoft.Extensions.Caching.Memory;
using DotNetEnv;
using RealEstate.API.Middleware;
using FluentValidation.AspNetCore;
using AutoMapper;
using RealEstate.API.Mappings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

// ==========================================
// 🔹 CARGA DE VARIABLES DE ENTORNO
// ==========================================
DotNetEnv.Env.Load();
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

// ==========================================
// 🔹 CONFIGURACIÓN DE SERVICIOS
// ==========================================

// MongoDB
var mongoConnectionString = Environment.GetEnvironmentVariable("MONGO_CONNECTION") 
    ?? "mongodb://localhost:27017";
var mongoDbName = Environment.GetEnvironmentVariable("MONGO_DATABASE") 
    ?? "RealEstateDb";
var mongoClient = new MongoClient(mongoConnectionString);
var database = mongoClient.GetDatabase(mongoDbName);
builder.Services.AddSingleton(database);

// Controladores + Newtonsoft + FluentValidation
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        options.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
    })
    .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<Program>());

// Caché en memoria
builder.Services.AddMemoryCache();

// Servicios
builder.Services.AddSingleton<PropertyService>(sp =>
{
    var cache = sp.GetRequiredService<IMemoryCache>();
    return new PropertyService(builder.Configuration, cache);
});
builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<JwtService>();

// AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// CORS global (permitir todo en desarrollo)
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
// 🔹 CONFIGURACIÓN JWT DESDE VARIABLES DE ENTORNO
// ==========================================
var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET") 
    ?? "ClaveSuperSecretaMuyLarga123!";
var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") 
    ?? "RealEstateAPI";
var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") 
    ?? "UsuariosAPI";
var expiryMinutes = Environment.GetEnvironmentVariable("JWT_EXPIRY_MINUTES") ?? "60";

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
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// ==========================================
// 🔹 CONSTRUCCIÓN DE LA APP
// ==========================================
var app = builder.Build();

// ==========================================
// 🔹 MIDDLEWARE
// ==========================================
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseMiddleware<ErrorHandlerMiddleware>();

app.UseAuthentication();   // 🔹 JWT
app.UseAuthorization();

// Mapear controladores
app.MapControllers();

// ==========================================
// 🔹 EJECUCIÓN
// ==========================================
app.Run();
