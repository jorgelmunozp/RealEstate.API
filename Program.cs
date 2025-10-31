using System.Text;
using System.Linq;
using DotNetEnv;
using MongoDB.Driver;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;
using System.IdentityModel.Tokens.Jwt;

// Core
using RealEstate.API.Middleware;
using RealEstate.API.Mappings;
using RealEstate.API.Infraestructure.Core.Services;

// Token / Auth / Password
using RealEstate.API.Modules.Token.Service;
using RealEstate.API.Modules.Token.Interface;
using RealEstate.API.Modules.Auth.Dto;
using RealEstate.API.Modules.Auth.Validator;
using RealEstate.API.Modules.Auth.Service;
using RealEstate.API.Modules.Auth.Interface;
using RealEstate.API.Modules.Password.Service;
using RealEstate.API.Modules.Password.Interface;

// User
using RealEstate.API.Modules.User.Dto;
using RealEstate.API.Modules.User.Validator;
using RealEstate.API.Modules.User.Service;
using RealEstate.API.Modules.User.Interface;

// Property / Owner / Image / Trace
using RealEstate.API.Modules.Property.Dto;
using RealEstate.API.Modules.Property.Validator;
using RealEstate.API.Modules.Property.Service;
using RealEstate.API.Modules.Property.Interface;
using RealEstate.API.Modules.Owner.Dto;
using RealEstate.API.Modules.Owner.Validator;
using RealEstate.API.Modules.Owner.Service;
using RealEstate.API.Modules.Owner.Interface;
using RealEstate.API.Modules.PropertyImage.Dto;
using RealEstate.API.Modules.PropertyImage.Validator;
using RealEstate.API.Modules.PropertyImage.Service;
using RealEstate.API.Modules.PropertyImage.Interface;
using RealEstate.API.Modules.PropertyTrace.Dto;
using RealEstate.API.Modules.PropertyTrace.Validator;
using RealEstate.API.Modules.PropertyTrace.Service;
using RealEstate.API.Modules.PropertyTrace.Interface;

// ===========================================================
// CONFIGURACIÓN BASE
// ===========================================================
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

// Evita el remapeo automático de claims (sub → nameidentifier)
JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

var config = builder.Configuration;

// ===========================================================
// MONGODB
// ===========================================================
var mongoConnection = config["MONGO_CONNECTION"] ?? "mongodb://localhost:27017";
var mongoDatabase = config["MONGO_DATABASE"] ?? "RealEstate";
if (string.IsNullOrWhiteSpace(mongoConnection)) throw new InvalidOperationException("La variable MONGO_CONNECTION no puede ser nula o vacía.");
if (string.IsNullOrWhiteSpace(mongoDatabase)) throw new InvalidOperationException("La variable MONGO_DATABASE no puede ser nula o vacía.");
Console.WriteLine($"[MongoDB] Conexión: {mongoConnection}");
Console.WriteLine($"[MongoDB] Base de datos: {mongoDatabase}");
builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnection));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(mongoDatabase));

// ===========================================================
// JSON GLOBAL (camelCase + case-insensitive)
// ===========================================================
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    o.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});

// ===========================================================
// JWT CONFIG (desde entorno)
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
// VALIDACIÓN GLOBAL (FluentValidation + Wrapper uniforme)
// ===========================================================
builder.Services.AddFluentValidationAutoValidation().AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<Program>(ServiceLifetime.Scoped);
FluentValidation.ValidatorOptions.Global.PropertyNameResolver = (type, member, expr) =>
{
    var name = member?.Name ?? expr?.ToString()?.Split('.').LastOrDefault();
    return string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];
};
builder.Services.Configure<ApiBehaviorOptions>(opt =>
{
    opt.InvalidModelStateResponseFactory = ctx =>
    {
        var Errors = ctx.ModelState.Where(x => x.Value?.Errors.Count > 0).SelectMany(v => v.Value!.Errors).Select(e => e.ErrorMessage).ToArray();
        var payload = ServiceResultWrapper<object>.Fail(Errors, statusCode: 400, message: "Errores de validación");
        return new BadRequestObjectResult(payload);
    };
});

// ===========================================================
// CACHÉ EN MEMORIA
// ===========================================================
builder.Services.AddMemoryCache();

// ===========================================================
// SWAGGER
// ===========================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ===========================================================
// INYECCIÓN DE DEPENDENCIAS (Servicios / Validadores)
// ===========================================================
// Auth / Token / Password
builder.Services.AddScoped<IValidator<LoginDto>, LoginDtoValidator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
// User
builder.Services.AddScoped<IValidator<UserDto>, UserDtoValidator>();
builder.Services.AddScoped<IUserService, UserService>();
// Owner
builder.Services.AddScoped<IValidator<OwnerDto>, OwnerDtoValidator>();
builder.Services.AddScoped<IOwnerService, OwnerService>();
// PropertyImage (antes de PropertyService)
builder.Services.AddScoped<IValidator<PropertyImageDto>, PropertyImageDtoValidator>();
builder.Services.AddScoped<IPropertyImageService, PropertyImageService>();
// PropertyTrace (antes de PropertyService)
builder.Services.AddScoped<IValidator<PropertyTraceDto>, PropertyTraceDtoValidator>();
builder.Services.AddScoped<IPropertyTraceService, PropertyTraceService>();
// Property principal
builder.Services.AddScoped<IValidator<PropertyDto>, PropertyDtoValidator>();
builder.Services.AddScoped<IPropertyService, PropertyService>();

// ===========================================================
// AUTOMAPPER (perfil de mapeos globales)
// ===========================================================
builder.Services.AddAutoMapper(typeof(AutoMapperProfile).Assembly);

// ===========================================================
// CORS
// ===========================================================
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// ===========================================================
// AUTENTICACIÓN JWT
// ===========================================================
var keyBytes = Encoding.UTF8.GetBytes(secretKey);
builder.Services.AddAuthentication(opt =>
{
    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(opt =>
{
    // Evita que ASP.NET Core remapee los claims (p.ej., "sub" → NameIdentifier)
    opt.MapInboundClaims = false;

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
// LOGGING
// ===========================================================
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(o =>
{
    o.TimestampFormat = "[HH:mm:ss] ";
    o.SingleLine = true;
});

// ===========================================================
// PIPELINE DE MIDDLEWARE
// ===========================================================
var app = builder.Build();


// ===========================================================
// STATUS CODE PAGES (respuestas JSON unificadas)
// ===========================================================
// app.UseStatusCodePages(async context =>
// {
//     var res = context.HttpContext.Response;
//     var code = res.StatusCode;
//     var msg = code switch
//     {
//         401 => "No autorizado",
//         403 => "Prohibido",
//         404 => "Recurso no encontrado",
//         405 => "Método no permitido",
//         415 => "Tipo de contenido no soportado",
//         _ => "Error inesperado"
//     };
//     var payload = ServiceResultWrapper<string>.Fail(msg, code);
//     res.ContentType = "application/json; charset=utf-8";
//     var json = System.Text.Json.JsonSerializer.Serialize(payload, new System.Text.Json.JsonSerializerOptions
//     {
//         PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
//         Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
//         WriteIndented = true
//     });
//     await res.WriteAsync(json);
// });

// Orden recomendado
app.UseMiddleware<LoggingMiddleware>();
app.UseMiddleware<ErrorHandlerMiddleware>();

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "RealEstate API v1");
        c.RoutePrefix = "swagger";
    });
}

app.MapControllers();
app.Run();
